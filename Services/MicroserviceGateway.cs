using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace JobCareerPlatform.Services
{
    public class MicroserviceResult<T>
    {
        public bool Succeeded { get; init; }
        public bool WasAttempted { get; init; }
        public int StatusCode { get; init; }
        public T? Value { get; init; }
        public string? Error { get; init; }

        public static MicroserviceResult<T> NotConfigured() =>
            new() { Succeeded = false, WasAttempted = false, Error = "Microservice endpoint is not configured." };

        public static MicroserviceResult<T> Failed(int statusCode, string error) =>
            new() { Succeeded = false, WasAttempted = true, StatusCode = statusCode, Error = error };

        public static MicroserviceResult<T> Success(int statusCode, T? value) =>
            new() { Succeeded = true, WasAttempted = true, StatusCode = statusCode, Value = value };
    }

    public interface IMicroserviceGateway
    {
        bool IsConfigured { get; }

        Task<MicroserviceResult<TResponse>> PostAsync<TResponse>(
            string path,
            object payload,
            CancellationToken cancellationToken = default);
    }

    public class MicroserviceGateway : IMicroserviceGateway
    {
        private readonly HttpClient _httpClient;
        private readonly MicroserviceOptions _options;
        private readonly ILogger<MicroserviceGateway> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MicroserviceGateway(
            HttpClient httpClient,
            IOptions<MicroserviceOptions> options,
            ILogger<MicroserviceGateway> logger)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClient;

            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-api-key");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            }
        }

        public bool IsConfigured => _options.IsConfigured;

        public async Task<MicroserviceResult<TResponse>> PostAsync<TResponse>(
            string path,
            object payload,
            CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogDebug("Microservice call to {Path} skipped: no base URL configured.", path);
                return MicroserviceResult<TResponse>.NotConfigured();
            }

            string url = $"{_options.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

            try
            {
                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(json, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using HttpResponseMessage response =
                    await _httpClient.PostAsync(url, content, cancellationToken);

                string body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Microservice {Url} returned {StatusCode}: {Body}",
                        url, (int)response.StatusCode, body);

                    return MicroserviceResult<TResponse>.Failed((int)response.StatusCode, body);
                }

                TResponse? value = string.IsNullOrWhiteSpace(body)
                    ? default
                    : JsonSerializer.Deserialize<TResponse>(body, JsonOptions);

                _logger.LogInformation(
                    "Microservice {Url} responded {StatusCode}.", url, (int)response.StatusCode);

                return MicroserviceResult<TResponse>.Success((int)response.StatusCode, value);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Microservice {Url} timed out after {Timeout}s.",
                    url, _options.TimeoutSeconds);

                return MicroserviceResult<TResponse>.Failed(504, "The microservice did not respond in time.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Microservice {Url} could not be reached.", url);
                return MicroserviceResult<TResponse>.Failed(503, ex.Message);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Microservice {Url} returned a body that could not be parsed.", url);
                return MicroserviceResult<TResponse>.Failed(502, ex.Message);
            }
        }
    }
}
