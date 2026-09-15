using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ResourceNotificationService;

public class Function
{
    private static readonly IAmazonSimpleNotificationService SnsClient =
        new AmazonSimpleNotificationServiceClient();

    private static readonly string TopicArn =
        Environment.GetEnvironmentVariable("CAREER_RESOURCE_TOPIC_ARN") ?? string.Empty;

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
            context.Logger.LogError("CAREER_RESOURCE_TOPIC_ARN environment variable is not set.");
            return Respond(500, new ResourceNotificationResponse
            {
                Published = false,
                Reason = "Notification topic is not configured."
            });
        }

        ResourceNotificationRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ResourceNotificationRequest>(
                request.Body ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Malformed request body: {ex.Message}");
            return Respond(400, new ResourceNotificationResponse
            {
                Published = false,
                Reason = "Request body is not valid JSON."
            });
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.Title))
        {
            return Respond(400, new ResourceNotificationResponse
            {
                Published = false,
                Reason = "Title is required."
            });
        }

        List<string> recipients = payload.MatchedJobSeekerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            context.Logger.LogInformation(
                $"Resource {payload.ResourceId} matched no job seekers; nothing published.");

            return Respond(200, new ResourceNotificationResponse
            {
                Published = false,
                RecipientCount = 0,
                Reason = "No job seekers matched this resource."
            });
        }

        var publishRequest = new PublishRequest
        {
            TopicArn = TopicArn,
            Subject = Truncate($"New Career Resource: {payload.Title}", 100),
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
                $"Published resource {payload.ResourceId} to {recipients.Count} matched job seeker(s). SNS MessageId={result.MessageId}");

            return Respond(200, new ResourceNotificationResponse
            {
                Published = true,
                RecipientCount = recipients.Count,
                MessageId = result.MessageId
            });
        }
        catch (AmazonSimpleNotificationServiceException ex)
        {
            context.Logger.LogError($"SNS publish failed: {ex.Message}");
            return Respond(502, new ResourceNotificationResponse
            {
                Published = false,
                RecipientCount = recipients.Count,
                Reason = "Notification service could not publish the message."
            });
        }
    }

    private static string BuildMessage(ResourceNotificationRequest payload)
    {
        var lines = new List<string>
        {
            "A new career resource has been published that matches your profile.",
            string.Empty,
            $"Title: {payload.Title}"
        };

        if (!string.IsNullOrWhiteSpace(payload.ResourceType))
        {
            lines.Add($"Type: {payload.ResourceType}");
        }

        if (!string.IsNullOrWhiteSpace(payload.Description))
        {
            lines.Add(string.Empty);
            lines.Add(payload.Description);
        }

        if (!string.IsNullOrWhiteSpace(payload.AttachmentUrl))
        {
            lines.Add(string.Empty);
            lines.Add($"Attachment: {payload.AttachmentUrl}");
        }

        lines.Add(string.Empty);
        lines.Add("Sign in to the Job & Career Platform to view this resource.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static APIGatewayProxyResponse Respond(int statusCode, ResourceNotificationResponse body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }
}

public class ResourceNotificationRequest
{
    public int ResourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ResourceType { get; set; }
    public string? AttachmentUrl { get; set; }
    public List<string> MatchedJobSeekerIds { get; set; } = new();
}

public class ResourceNotificationResponse
{
    public bool Published { get; set; }
    public int RecipientCount { get; set; }
    public string? MessageId { get; set; }
    public string? Reason { get; set; }
}
