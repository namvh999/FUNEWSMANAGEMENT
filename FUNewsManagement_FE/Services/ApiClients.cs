using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FUNewsManagement_FE.Models;

namespace FUNewsManagement_FE.Services
{
    // =========================================================================
    // Core API Client
    // =========================================================================
    public class CoreApiClient
    {
        private readonly HttpClient _client;
        private readonly OfflineDataService _offlineService;
        private readonly JsonSerializerOptions _jsonOptions;

        public CoreApiClient(IHttpClientFactory clientFactory, OfflineDataService offlineService)
        {
            _client = clientFactory.CreateClient("CoreApi");
            _offlineService = offlineService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<T?> GetAsync<T>(string endpoint, string cacheKey = "")
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    _offlineService.IsOfflineMode = false;
                    var stream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
                    if (!string.IsNullOrEmpty(cacheKey) && data != null)
                    {
                        _offlineService.SaveCache(cacheKey, data);
                    }
                    return data;
                }
            }
            catch (Exception)
            {
                _offlineService.IsOfflineMode = true;
            }

            // Fallback to offline cache
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return _offlineService.GetCache<T>(cacheKey);
            }
            return default;
        }

        public async Task<(bool success, string message, T? data)> PostAsync<T>(string endpoint, object payload)
        {
            if (_offlineService.IsOfflineMode)
            {
                return (false, "Hệ thống đang ở chế độ Ngoại tuyến (Offline Mode). Các thao tác thêm/sửa/xóa tạm thời bị vô hiệu hóa.", default);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(endpoint, content);
                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    T? data = default;
                    try { data = JsonSerializer.Deserialize<T>(resString, _jsonOptions); } catch { }
                    return (true, "Thao tác thành công!", data);
                }

                return (false, ExtractErrorMessage(resString), default);
            }
            catch (Exception ex)
            {
                _offlineService.IsOfflineMode = true;
                return (false, "Không thể kết nối đến Core API: " + ex.Message, default);
            }
        }

        public async Task<(bool success, string message)> PutAsync(string endpoint, object payload)
        {
            if (_offlineService.IsOfflineMode)
            {
                return (false, "Hệ thống đang ở chế độ Ngoại tuyến (Offline Mode). Các thao tác cập nhật bị vô hiệu hóa.");
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _client.PutAsync(endpoint, content);
                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật thành công!");
                }
                return (false, ExtractErrorMessage(resString));
            }
            catch (Exception ex)
            {
                _offlineService.IsOfflineMode = true;
                return (false, "Lỗi kết nối API: " + ex.Message);
            }
        }

        public async Task<(bool success, string message)> DeleteAsync(string endpoint)
        {
            if (_offlineService.IsOfflineMode)
            {
                return (false, "Hệ thống đang ở chế độ Ngoại tuyến (Offline Mode). Các thao tác xóa bị vô hiệu hóa.");
            }

            try
            {
                var response = await _client.DeleteAsync(endpoint);
                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Xóa thành công!");
                }
                return (false, ExtractErrorMessage(resString));
            }
            catch (Exception ex)
            {
                _offlineService.IsOfflineMode = true;
                return (false, "Lỗi kết nối API: " + ex.Message);
            }
        }

        public async Task<(bool success, string url, string message)> UploadImageAsync(IFormFile file)
        {
            if (_offlineService.IsOfflineMode)
            {
                return (false, "", "Hệ thống đang offline, không thể tải ảnh.");
            }

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                content.Add(new StreamContent(stream), "file", file.FileName);

                var response = await _client.PostAsync("/api/news/upload-image", content);
                var resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(resString);
                    var url = doc.RootElement.GetProperty("url").GetString() ?? "";
                    return (true, url, "Tải ảnh thành công!");
                }
                return (false, "", ExtractErrorMessage(resString));
            }
            catch (Exception ex)
            {
                return (false, "", "Lỗi upload ảnh: " + ex.Message);
            }
        }

        private static string ExtractErrorMessage(string resString)
        {
            try
            {
                using var doc = JsonDocument.Parse(resString);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString() ?? "Đã xảy ra lỗi.";
                }
            }
            catch { }
            return string.IsNullOrWhiteSpace(resString) ? "Lỗi không xác định từ máy chủ." : resString;
        }
    }

    // =========================================================================
    // Analytics API Client
    // =========================================================================
    public class AnalyticsApiClient
    {
        private readonly HttpClient _client;
        private readonly OfflineDataService _offlineService;
        private readonly JsonSerializerOptions _jsonOptions;

        public AnalyticsApiClient(IHttpClientFactory clientFactory, OfflineDataService offlineService)
        {
            _client = clientFactory.CreateClient("AnalyticsApi");
            _offlineService = offlineService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<T?> GetAsync<T>(string endpoint, string cacheKey = "")
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
                    if (!string.IsNullOrEmpty(cacheKey) && data != null)
                    {
                        _offlineService.SaveCache(cacheKey, data);
                    }
                    return data;
                }
            }
            catch (Exception)
            {
                _offlineService.IsOfflineMode = true;
            }

            if (!string.IsNullOrEmpty(cacheKey))
            {
                return _offlineService.GetCache<T>(cacheKey);
            }
            return default;
        }

        public async Task<byte[]?> DownloadReportAsync(string endpoint)
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch { }
            return null;
        }
    }

    // =========================================================================
    // AI API Client
    // =========================================================================
    public class AiApiClient
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public AiApiClient(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient("AiApi");
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<AiTagDto>> SuggestTagsAsync(string content)
        {
            try
            {
                var payload = new { content, maxSuggestions = 5 };
                var response = await _client.PostAsJsonAsync("/api/ai/suggest-tags", payload);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<List<AiTagDto>>(stream, _jsonOptions) ?? new();
                }
            }
            catch { }
            return new List<AiTagDto>();
        }

        public async Task LearnTagAsync(string tagName)
        {
            try
            {
                await _client.PostAsJsonAsync("/api/ai/learn-tag", new { tagName });
            }
            catch { }
        }
    }
}
