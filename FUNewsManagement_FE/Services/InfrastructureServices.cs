using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FUNewsManagement_FE.Services
{
    // =========================================================================
    // DelegatingHandler: Automatically attaches JWT and handles token refresh
    // =========================================================================
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;

        public AuthTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var token = session?.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // If 401 Unauthorized received, attempt Refresh Token Flow
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && session != null)
            {
                var refreshToken = session.GetString("refresh_token");
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))
                {
                    var refreshed = await TryRefreshTokenAsync(token, refreshToken, session);
                    if (refreshed)
                    {
                        var newToken = session.GetString("access_token");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        return await base.SendAsync(request, cancellationToken);
                    }
                }
            }

            return response;
        }

        private async Task<bool> TryRefreshTokenAsync(string expiredToken, string refreshToken, ISession session)
        {
            try
            {
                var coreApiUrl = _config["ApiUrls:CoreApi"] ?? "http://localhost:5001";
                using var client = new HttpClient();
                var refreshPayload = new { accessToken = expiredToken, refreshToken };
                var content = new StringContent(JsonSerializer.Serialize(refreshPayload), Encoding.UTF8, "application/json");

                var res = await client.PostAsync($"{coreApiUrl}/api/auth/refresh", content);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var newAccessToken = doc.RootElement.GetProperty("accessToken").GetString();
                    var newRefreshToken = doc.RootElement.GetProperty("refreshToken").GetString();

                    if (!string.IsNullOrEmpty(newAccessToken))
                    {
                        session.SetString("access_token", newAccessToken);
                    }
                    if (!string.IsNullOrEmpty(newRefreshToken))
                    {
                        session.SetString("refresh_token", newRefreshToken);
                    }
                    return true;
                }
            }
            catch { }

            return false;
        }
    }

    // =========================================================================
    // Offline Data & Cache Service
    // =========================================================================
    public class OfflineDataService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<OfflineDataService> _logger;
        public bool IsOfflineMode { get; set; } = false;

        public OfflineDataService(IWebHostEnvironment env, ILogger<OfflineDataService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public string CacheFilePath =>
            Path.Combine(_env.ContentRootPath, "App_Data", "offline_cache.json");

        public void SaveCache(string key, object data)
        {
            try
            {
                var dir = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                Dictionary<string, string> store = new();
                if (File.Exists(CacheFilePath))
                {
                    var existingJson = File.ReadAllText(CacheFilePath);
                    store = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson) ?? new();
                }

                store[key] = JsonSerializer.Serialize(data);
                File.WriteAllText(CacheFilePath, JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist offline cache.");
            }
        }

        public T? GetCache<T>(string key)
        {
            try
            {
                if (!File.Exists(CacheFilePath)) return default;
                var json = File.ReadAllText(CacheFilePath);
                var store = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (store != null && store.TryGetValue(key, out var dataJson))
                {
                    return JsonSerializer.Deserialize<T>(dataJson);
                }
            }
            catch { }
            return default;
        }
    }

    // =========================================================================
    // Background Worker (HostedService): refreshes cached data every 6 hours
    // =========================================================================
    public class BackgroundCacheWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundCacheWorker> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(6);

        public BackgroundCacheWorker(IServiceProvider serviceProvider, ILogger<BackgroundCacheWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundCacheWorker started. Scheduled to refresh cached data every 6 hours.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshCachedDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during periodic cache refresh.");
                }

                await Task.Delay(_period, stoppingToken);
            }
        }

        private async Task RefreshCachedDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var offlineService = scope.ServiceProvider.GetRequiredService<OfflineDataService>();

            try
            {
                // Refresh Categories
                var coreClient = factory.CreateClient("CoreApi");
                var catRes = await coreClient.GetAsync("/api/category");
                if (catRes.IsSuccessStatusCode)
                {
                    var catData = await catRes.Content.ReadAsStringAsync();
                    offlineService.SaveCache("categories", catData);
                }

                // Refresh Dashboard stats
                var analyticsClient = factory.CreateClient("AnalyticsApi");
                var dashRes = await analyticsClient.GetAsync("/api/analytics/dashboard");
                if (dashRes.IsSuccessStatusCode)
                {
                    var dashData = await dashRes.Content.ReadAsStringAsync();
                    offlineService.SaveCache("dashboard", dashData);
                }

                _logger.LogInformation("Successfully updated background cache at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Background cache refresh could not reach API services: {msg}", ex.Message);
            }
        }
    }
}
