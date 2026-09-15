using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace JobAlertService;

public class Function
{
    private static readonly IAmazonSimpleNotificationService SnsClient =
        new AmazonSimpleNotificationServiceClient();

    private static readonly string TopicArn =
        Environment.GetEnvironmentVariable("JOB_ALERT_TOPIC_ARN") ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(TopicArn))
        {
            context.Logger.LogError("JOB_ALERT_TOPIC_ARN environment variable is not set.");
            return Respond(500, new JobAlertResponse
            {
                Published = false,
                Reason = "Job alert topic is not configured."
            });
        }

        JobAlertRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<JobAlertRequest>(
                request.Body ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Malformed request body: {ex.Message}");
            return Respond(400, new JobAlertResponse
            {
                Published = false,
                Reason = "Request body is not valid JSON."
            });
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.JobTitle))
        {
            return Respond(400, new JobAlertResponse
            {
                Published = false,
                Reason = "JobTitle is required."
            });
        }

        List<string> recipients = payload.MatchedJobSeekerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            context.Logger.LogInformation(
                $"Job {payload.JobId} matched no job seekers; nothing published.");

            return Respond(200, new JobAlertResponse
            {
                Published = false,
                RecipientCount = 0,
                Reason = "No job seekers matched this vacancy."
            });
        }

        var publishRequest = new PublishRequest
        {
            TopicArn = TopicArn,
            Subject = Truncate($"New Job Opening: {payload.JobTitle}", 100),
            Message = BuildMessage(payload),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["jobseeker_id"] = new MessageAttributeValue
                {
                    DataType = "String.Array",
                    StringValue = JsonSerializer.Serialize(recipients)
                }
            }
        };

        try
        {
            PublishResponse result = await SnsClient.PublishAsync(publishRequest);

            context.Logger.LogInformation(
                $"Published job alert {payload.JobId} to {recipients.Count} matched job seeker(s). SNS MessageId={result.MessageId}");

            return Respond(200, new JobAlertResponse
            {
                Published = true,
                RecipientCount = recipients.Count,
                MessageId = result.MessageId
            });
        }
        catch (AmazonSimpleNotificationServiceException ex)
        {
            context.Logger.LogError($"SNS publish failed: {ex.Message}");
            return Respond(502, new JobAlertResponse
            {
                Published = false,
                RecipientCount = recipients.Count,
                Reason = "Job alert service could not publish the message."
            });
        }
    }

    private static string BuildMessage(JobAlertRequest payload)
    {
        var lines = new List<string>
        {
            "A new job vacancy matching your profile has just been published.",
            string.Empty,
            $"Position: {payload.JobTitle}"
        };

        if (!string.IsNullOrWhiteSpace(payload.CompanyName))
        {
            lines.Add($"Company: {payload.CompanyName}");
        }

        if (!string.IsNullOrWhiteSpace(payload.Location))
        {
            lines.Add($"Location: {payload.Location}");
        }

        if (!string.IsNullOrWhiteSpace(payload.EmploymentType))
        {
            lines.Add($"Employment type: {payload.EmploymentType}");
        }

        if (!string.IsNullOrWhiteSpace(payload.JobCategory))
        {
            lines.Add($"Category: {payload.JobCategory}");
        }

        if (payload.SalaryMin.HasValue && payload.SalaryMax.HasValue)
        {
            lines.Add($"Salary range: {payload.SalaryMin:N0} - {payload.SalaryMax:N0}");
        }

        if (!string.IsNullOrWhiteSpace(payload.RequiredSkills))
        {
            lines.Add($"Required skills: {payload.RequiredSkills}");
        }

        lines.Add(string.Empty);
        lines.Add("Sign in to the Job & Career Platform to view the full posting and apply.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static APIGatewayProxyResponse Respond(int statusCode, JobAlertResponse body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }
}

public class JobAlertRequest
{
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? JobCategory { get; set; }
    public string? RequiredSkills { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public List<string> MatchedJobSeekerIds { get; set; } = new();
}

public class JobAlertResponse
{
    public bool Published { get; set; }
    public int RecipientCount { get; set; }
    public string? MessageId { get; set; }
    public string? Reason { get; set; }
}
