using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CompanyMediaService;

public class Function
{
    private static readonly IAmazonS3 S3Client = new AmazonS3Client();

    private static readonly string BucketName =
        Environment.GetEnvironmentVariable("LOGO_BUCKET_NAME") ?? string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MaxBytes = 1_000_000;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(BucketName))
        {
            context.Logger.LogError("LOGO_BUCKET_NAME environment variable is not set.");
            return Respond(500, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "Logo bucket is not configured."
            });
        }

        CompanyMediaRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CompanyMediaRequest>(
                request.Body ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Malformed request body: {ex.Message}");
            return Respond(400, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "Request body is not valid JSON."
            });
        }

        if (payload == null
            || string.IsNullOrWhiteSpace(payload.FileName)
            || string.IsNullOrWhiteSpace(payload.ContentBase64))
        {
            return Respond(400, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "FileName and ContentBase64 are required."
            });
        }

        if (!AllowedContentTypes.Contains(payload.ContentType ?? string.Empty))
        {
            return Respond(415, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "Only JPG, PNG, GIF or WEBP images are accepted."
            });
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload.ContentBase64);
        }
        catch (FormatException)
        {
            return Respond(400, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "File content is not valid base64."
            });
        }

        if (bytes.Length == 0 || bytes.Length > MaxBytes)
        {
            return Respond(413, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "Logo must be between 1 byte and 1 MB."
            });
        }

        string key = BuildKey(payload);

        try
        {
            using var stream = new MemoryStream(bytes);

            await S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream,
                ContentType = payload.ContentType,
                CannedACL = S3CannedACL.PublicRead
            });

            string url = $"https://{BucketName}.s3.amazonaws.com/{Uri.EscapeDataString(key).Replace("%2F", "/")}";

            context.Logger.LogInformation(
                $"Stored logo for company profile {payload.CompanyProfileId} at s3://{BucketName}/{key}");

            return Respond(200, new MediaUploadResponse
            {
                Uploaded = true,
                Key = key,
                Url = url
            });
        }
        catch (AmazonS3Exception ex)
        {
            context.Logger.LogError($"S3 write failed: {ex.Message}");
            return Respond(502, new MediaUploadResponse
            {
                Uploaded = false,
                Reason = "Logo could not be written to storage."
            });
        }
    }

    private static string BuildKey(CompanyMediaRequest payload)
    {
        string company = Sanitise(payload.CompanyName);
        string fileName = Sanitise(Path.GetFileName(payload.FileName));

        return $"logos/id-{payload.CompanyProfileId}-company-{company}/{fileName}";
    }

    private static string Sanitise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unnamed";
        }

        char[] cleaned = value.Trim()
            .Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-')
            .ToArray();

        return new string(cleaned);
    }

    private static APIGatewayProxyResponse Respond(int statusCode, MediaUploadResponse body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }
}

public class CompanyMediaRequest
{
    public int CompanyProfileId { get; set; }
    public string? CompanyName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string ContentBase64 { get; set; } = string.Empty;
}

public class MediaUploadResponse
{
    public bool Uploaded { get; set; }
    public string? Key { get; set; }
    public string? Url { get; set; }
    public string? Reason { get; set; }
}
