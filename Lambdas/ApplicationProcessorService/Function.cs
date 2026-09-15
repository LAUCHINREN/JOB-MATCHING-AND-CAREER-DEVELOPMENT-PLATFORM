using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ApplicationProcessorService;

public class Function
{
    private static readonly IAmazonSimpleNotificationService SnsClient =
        new AmazonSimpleNotificationServiceClient();

    private static readonly string TopicArn =
        Environment.GetEnvironmentVariable("APPLICATION_STATUS_TOPIC_ARN") ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        if (string.IsNullOrWhiteSpace(TopicArn))
        {
            context.Logger.LogError("APPLICATION_STATUS_TOPIC_ARN environment variable is not set.");

            return new SQSBatchResponse(sqsEvent.Records
                .Select(r => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = r.MessageId })
                .ToList());
        }

        foreach (SQSEvent.SQSMessage record in sqsEvent.Records)
        {
            try
            {
                await ProcessRecordAsync(record, context);
            }
            catch (JsonException ex)
            {
                context.Logger.LogError(
                    $"Discarding unparseable message {record.MessageId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Failed to process message {record.MessageId}: {ex.Message}");
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        context.Logger.LogInformation(
            $"Processed {sqsEvent.Records.Count} message(s), {failures.Count} returned for retry.");

        return new SQSBatchResponse(failures);
    }

    private static async Task ProcessRecordAsync(SQSEvent.SQSMessage record, ILambdaContext context)
    {
        ApplicationSubmittedEvent? payload =
            JsonSerializer.Deserialize<ApplicationSubmittedEvent>(record.Body, JsonOptions);

        if (payload == null || string.IsNullOrWhiteSpace(payload.JobSeekerId))
        {
            throw new JsonException("Message body did not contain a usable application event.");
        }

        var publishRequest = new PublishRequest
        {
            TopicArn = TopicArn,
            Subject = Truncate($"Application received: {payload.JobTitle}", 100),
            Message = BuildMessage(payload),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["jobseeker_id"] = new MessageAttributeValue
                {
                    DataType = "String.Array",
                    StringValue = JsonSerializer.Serialize(new[] { payload.JobSeekerId })
                }
            }
        };

        PublishResponse result = await SnsClient.PublishAsync(publishRequest);

        context.Logger.LogInformation(
            $"Confirmation sent for application {payload.ApplicationId} to job seeker {payload.JobSeekerId}. SNS MessageId={result.MessageId}");
    }

    private static string BuildMessage(ApplicationSubmittedEvent payload)
    {
        var lines = new List<string>
        {
            $"Hello{(string.IsNullOrWhiteSpace(payload.JobSeekerName) ? string.Empty : " " + payload.JobSeekerName)},",
            string.Empty,
            "Your job application has been received.",
            string.Empty,
            $"Position: {payload.JobTitle}"
        };

        if (!string.IsNullOrWhiteSpace(payload.EmployerName))
        {
            lines.Add($"Employer: {payload.EmployerName}");
        }

        lines.Add($"Reference: APP-{payload.ApplicationId}");
        lines.Add($"Submitted: {payload.AppliedDate:dd MMM yyyy HH:mm}");
        lines.Add(string.Empty);
        lines.Add("You can track the status of this application from your dashboard.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}

public class ApplicationSubmittedEvent
{
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? EmployerName { get; set; }
    public string JobSeekerId { get; set; } = string.Empty;
    public string? JobSeekerName { get; set; }
    public DateTime AppliedDate { get; set; }
}
