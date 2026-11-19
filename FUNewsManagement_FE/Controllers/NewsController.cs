using FUNewsManagement_FE.Models;
using FUNewsManagement_FE.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_FE.Controllers
{
    public class NewsController : Controller
    {
        private readonly CoreApiClient _coreClient;
        private readonly AnalyticsApiClient _analyticsClient;
        private readonly AiApiClient _aiClient;

        public NewsController(CoreApiClient coreClient, AnalyticsApiClient analyticsClient, AiApiClient aiClient)
        {
            _coreClient = coreClient;
            _analyticsClient = analyticsClient;
            _aiClient = aiClient;
        }

        // GET: /News
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            bool? status,
            string? sortBy = "date_desc",
            int page = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
            {
                return RedirectToAction("Login", "Auth");
            }

            var queryParams = new List<string>
            {
                $"page={page}",
                "pageSize=10",
                $"sortBy={sortBy ?? "date_desc"}"
            };

            if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (categoryId.HasValue && categoryId.Value > 0) queryParams.Add($"categoryId={categoryId.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");

            var endpoint = $"/api/news?{string.Join("&", queryParams)}";
            var result = await _coreClient.GetAsync<PagedNewsResult>(endpoint, $"news_page_{page}") ?? new PagedNewsResult();
            var categories = await _coreClient.GetAsync<List<CategoryViewModel>>("/api/category", "categories") ?? new List<CategoryViewModel>();
            var allTags = await _coreClient.GetAsync<List<TagViewModel>>("/api/tag", "tags") ?? new List<TagViewModel>();

            ViewBag.Categories = categories;
            ViewBag.AllTags = allTags;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.SortBy = sortBy;

            return View(result);
        }

        // GET: /News/Detail/{id}
        // Section 4.5: News Detail + up to 3 related articles based on category or tag
        public async Task<IActionResult> Detail(int id)
        {
            var article = await _coreClient.GetAsync<NewsArticleViewModel>($"/api/news/{id}");
            if (article == null)
            {
                TempData["Error"] = $"Không tìm thấy bài viết #{id}.";
                return RedirectToAction("Index");
            }

            // Get recommendations from Analytics API
            var recommendations = await _analyticsClient.GetAsync<List<RecommendedArticleViewModel>>($"/api/recommend/{id}")
                                  ?? new List<RecommendedArticleViewModel>();

            ViewBag.Recommendations = recommendations;
            return View(article);
        }

        // POST: /News/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            string newsTitle,
            string? headline,
            string newsContent,
            string? newsSource,
            int categoryId,
            bool newsStatus,
            string? featuredImage,
            List<int> tagIds,
            List<string>? learnedTags)
        {
            var payload = new
            {
                newsTitle,
                headline,
                newsContent,
                newsSource,
                categoryId,
                newsStatus,
                featuredImage,
                tagIds
            };

            var (success, message, _) = await _coreClient.PostAsync<object>("/api/news", payload);
            if (success)
            {
                // Teach AI the selected tags for its learning cache
                if (learnedTags != null && learnedTags.Any())
                {
                    foreach (var tag in learnedTags)
                    {
                        _ = _aiClient.LearnTagAsync(tag);
                    }
                }

                TempData["Success"] = "Đăng bài viết mới thành công!";
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }

        // POST: /News/Edit/{id}
        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            string newsTitle,
            string? headline,
            string newsContent,
            string? newsSource,
            int categoryId,
            bool newsStatus,
            string? featuredImage,
            List<int> tagIds)
        {
            var payload = new
            {
                newsTitle,
                headline,
                newsContent,
                newsSource,
                categoryId,
                newsStatus,
                featuredImage,
                tagIds
            };

            var (success, message) = await _coreClient.PutAsync($"/api/news/{id}", payload);
            if (success)
            {
                TempData["Success"] = $"Cập nhật bài viết #{id} thành công!";
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }

        // POST: /News/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _coreClient.DeleteAsync($"/api/news/{id}");
            if (success)
            {
                TempData["Success"] = $"Đã xóa bài viết #{id} thành công!";
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }

        // AJAX POST: /News/SuggestTags
        // Section 4.9: AI Tag Suggestion
        [HttpPost]
        public async Task<IActionResult> SuggestTags([FromBody] SuggestTagRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Content))
            {
                return BadRequest(new { message = "Nội dung bài viết không được để trống." });
            }

            var suggestions = await _aiClient.SuggestTagsAsync(req.Content);
            return Json(suggestions);
        }

        // AJAX POST: /News/UploadImage
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn một file ảnh hợp lệ." });
            }

            var (success, url, message) = await _coreClient.UploadImageAsync(file);
            if (success)
            {
                return Json(new { success = true, url, message });
            }

            return BadRequest(new { success = false, message });
        }
    }

    public class SuggestTagRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
