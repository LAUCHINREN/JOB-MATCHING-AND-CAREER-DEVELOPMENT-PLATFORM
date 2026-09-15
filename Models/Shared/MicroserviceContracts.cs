namespace JobCareerPlatform.Models
{
    // Response shapes returned by the Task #2 Lambda microservices through Amazon API Gateway.
    // These are intentionally separate, minimal copies of the contract rather than a shared
    // assembly: the whole point of the microservice split is that each service can change its
    // internals independently, so the monolith depends only on the wire format it actually reads.

    /// <summary>Returned by resource-notification-service (Career Advisor).</summary>
    public class ResourceNotificationResult
    {
        public bool Published { get; set; }
        public int RecipientCount { get; set; }
        public string? MessageId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Returned by application-intake-service (Job Seeker).</summary>
    public class ApplicationIntakeResult
    {
        public bool Accepted { get; set; }
        public string? MessageId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Returned by job-alert-service (Employer).</summary>
    public class JobAlertResult
    {
        public bool Published { get; set; }
        public int RecipientCount { get; set; }
        public string? MessageId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Returned by company-media-service (Employer).</summary>
    public class CompanyMediaResult
    {
        public bool Uploaded { get; set; }
        public string? Key { get; set; }
        public string? Url { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Returned by analytics-report-service (Administrator).</summary>
    public class AnalyticsReportResult
    {
        public bool Generated { get; set; }
        public string? CsvKey { get; set; }
        public string? JsonKey { get; set; }
        public string? DownloadUrl { get; set; }
        public int ExpiresInHours { get; set; }
        public string? Reason { get; set; }
    }
}
