using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ApplicationIntakeService;

public class Function
{
    private static readonly IAmazonSQS SqsClient = new AmazonSQSClient();

    private static readonly string QueueUrl =
        Environment.GetEnvironmentVariable("APPLICATION_QUEUE_URL") ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(QueueUrl))
        {
            context.Logger.LogError("APPLICATION_QUEUE_URL environment variable is not set.");
            return Respond(500, new IntakeResponse
            {
                Accepted = false,
                Reason = "Application queue is not configured."
            });
        }

        ApplicationSubmittedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ApplicationSubmittedEvent>(
                request.Body ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Malformed request body: {ex.Message}");
            return Respond(400, new IntakeResponse
            {
                Accepted = false,
                Reason = "Request body is not valid JSON."
            });
        }

        if (payload == null || payload.ApplicationId <= 0 || string.IsNullOrWhiteSpace(payload.JobSeekerId))
        {
            return Respond(400, new IntakeResponse
            {
                Accepted = false,
                Reason = "ApplicationId and JobSeekerId are required."
            });
        }

        var sendRequest = new SendMessageRequest
        {
            QueueUrl = QueueUrl,
            MessageBody = JsonSerializer.Serialize(payload),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "ApplicationSubmitted"
                }
            }
        };

        try
        {
            SendMessageResponse result = await SqsClient.SendMessageAsync(sendRequest);

            context.Logger.LogInformation(
                $"Queued application {payload.ApplicationId} for job seeker {payload.JobSeekerId}. SQS MessageId={result.MessageId}");

            return Respond(202, new IntakeResponse
            {
                Accepted = true,
                MessageId = result.MessageId
            });
        }
        catch (AmazonSQSException ex)
        {
            context.Logger.LogError($"SQS send failed: {ex.Message}");
            return Respond(502, new IntakeResponse
            {
                Accepted = false,
                Reason = "Application queue could not accept the message."
            });
        }
    }

    private static APIGatewayProxyResponse Respond(int statusCode, IntakeResponse body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
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

public class IntakeResponse
{
    public bool Accepted { get; set; }
    public string? MessageId { get; set; }
    public string? Reason { get; set; }
}
