namespace JobCareerPlatform.Services
{
    public class MicroserviceOptions
    {
        public const string SectionName = "Microservices";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public string ResourceNotificationPath { get; set; } = "/resource-notifications";

        public string JobApplicationPath { get; set; } = "/job-applications";

        public string JobAlertPath { get; set; } = "/job-alerts";

        public string CompanyMediaPath { get; set; } = "/company-media";

        public string AnalyticsReportPath { get; set; } = "/analytics-reports";

        public int TimeoutSeconds { get; set; } = 10;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
