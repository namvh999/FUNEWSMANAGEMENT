using FUNewsManagement_FE.Models;
using FUNewsManagement_FE.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_FE.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AnalyticsApiClient _analyticsClient;
        private readonly CoreApiClient _coreClient;

        public DashboardController(AnalyticsApiClient analyticsClient, CoreApiClient coreClient)
        {
            _analyticsClient = analyticsClient;
            _coreClient = coreClient;
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            int? categoryId,
            int? authorId,
            bool? status)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
            {
                return RedirectToAction("Login", "Auth");
            }

            var queryParams = new List<string>();
            if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            if (categoryId.HasValue && categoryId.Value > 0) queryParams.Add($"categoryId={categoryId.Value}");
            if (authorId.HasValue && authorId.Value > 0) queryParams.Add($"authorId={authorId.Value}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");

            var qs = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"/api/analytics/dashboard{qs}";

            var stats = await _analyticsClient.GetAsync<DashboardViewModel>(endpoint, "dashboard") ?? new DashboardViewModel();
            var categories = await _coreClient.GetAsync<List<CategoryViewModel>>("/api/category", "categories") ?? new List<CategoryViewModel>();

            ViewBag.Categories = categories;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedStatus = status;

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Export(DateTime? fromDate, DateTime? toDate, int? categoryId)
        {
            var queryParams = new List<string>();
            if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            if (categoryId.HasValue && categoryId.Value > 0) queryParams.Add($"categoryId={categoryId.Value}");

            var qs = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"/api/analytics/export{qs}";

            var bytes = await _analyticsClient.DownloadReportAsync(endpoint);
            if (bytes == null || bytes.Length == 0)
            {
                TempData["Error"] = "Không thể xuất báo cáo Excel lúc này hoặc hệ thống đang ngoại tuyến.";
                return RedirectToAction("Index");
            }

            var fileName = $"FUNews_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
