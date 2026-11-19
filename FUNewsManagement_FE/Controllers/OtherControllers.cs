using FUNewsManagement_FE.Models;
using FUNewsManagement_FE.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_FE.Controllers
{
    // =========================================================================
    // Category Management Controller (Section 4.6)
    // =========================================================================
    public class CategoryController : Controller
    {
        private readonly CoreApiClient _coreClient;

        public CategoryController(CoreApiClient coreClient)
        {
            _coreClient = coreClient;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
                return RedirectToAction("Login", "Auth");

            var categories = await _coreClient.GetAsync<List<CategoryViewModel>>("/api/category", "categories") ?? new();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string categoryName, string? categoryDescription, int? parentCategoryID, bool isActive = true)
        {
            var payload = new { categoryName, categoryDescription, parentCategoryID, isActive };
            var (success, message, _) = await _coreClient.PostAsync<object>("/api/category", payload);

            if (success) TempData["Success"] = "Tạo mới chuyên mục thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string categoryName, string? categoryDescription, int? parentCategoryID, bool isActive = true)
        {
            var payload = new { categoryName, categoryDescription, parentCategoryID, isActive };
            var (success, message) = await _coreClient.PutAsync($"/api/category/{id}", payload);

            if (success) TempData["Success"] = "Cập nhật chuyên mục thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (success, message) = await _coreClient.PutAsync($"/api/category/{id}/toggle-active", new { });
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _coreClient.DeleteAsync($"/api/category/{id}");
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }
    }

    // =========================================================================
    // Tag Management Controller (Section 4.7)
    // =========================================================================
    public class TagController : Controller
    {
        private readonly CoreApiClient _coreClient;

        public TagController(CoreApiClient coreClient)
        {
            _coreClient = coreClient;
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
                return RedirectToAction("Login", "Auth");

            List<TagViewModel> tags;
            if (!string.IsNullOrWhiteSpace(search))
            {
                tags = await _coreClient.GetAsync<List<TagViewModel>>($"/api/tag/search?q={Uri.EscapeDataString(search)}") ?? new();
            }
            else
            {
                tags = await _coreClient.GetAsync<List<TagViewModel>>("/api/tag", "tags") ?? new();
            }

            ViewBag.Search = search;
            return View(tags);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string tagName, string? note)
        {
            var payload = new { tagName, note };
            var (success, message, _) = await _coreClient.PostAsync<object>("/api/tag", payload);

            if (success) TempData["Success"] = "Tạo mới thẻ tag thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string tagName, string? note)
        {
            var payload = new { tagName, note };
            var (success, message) = await _coreClient.PutAsync($"/api/tag/{id}", payload);

            if (success) TempData["Success"] = "Cập nhật thẻ tag thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _coreClient.DeleteAsync($"/api/tag/{id}");
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Articles(int id)
        {
            var data = await _coreClient.GetAsync<TagArticlesResponse>($"/api/tag/{id}/articles");
            if (data == null)
            {
                TempData["Error"] = $"Không tìm thấy thẻ tag #{id}.";
                return RedirectToAction("Index");
            }
            return View(data);
        }
    }

    public class TagArticlesResponse
    {
        public TagViewModel Tag { get; set; } = new();
        public List<NewsArticleViewModel> Articles { get; set; } = new();
    }

    // =========================================================================
    // Account Management Controller (Section 4.8 - Admin)
    // =========================================================================
    public class AccountController : Controller
    {
        private readonly CoreApiClient _coreClient;

        public AccountController(CoreApiClient coreClient)
        {
            _coreClient = coreClient;
        }

        public async Task<IActionResult> Index(int? role)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
                return RedirectToAction("Login", "Auth");

            if (HttpContext.Session.GetString("user_role") != "Admin")
            {
                TempData["Error"] = "Chỉ có Quản trị viên (Admin) mới có quyền truy cập trang quản lý tài khoản.";
                return RedirectToAction("Index", "News");
            }

            var endpoint = role.HasValue ? $"/api/account?role={role.Value}" : "/api/account";
            var accounts = await _coreClient.GetAsync<List<AccountViewModel>>(endpoint) ?? new();

            ViewBag.SelectedRole = role;
            return View(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string accountName, string accountEmail, int accountRole, string accountPassword)
        {
            var payload = new { accountName, accountEmail, accountRole, accountPassword };
            var (success, message, _) = await _coreClient.PostAsync<object>("/api/account", payload);

            if (success) TempData["Success"] = "Tạo mới tài khoản thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string accountName, string accountEmail, int accountRole)
        {
            var payload = new { accountName, accountEmail, accountRole };
            var (success, message) = await _coreClient.PutAsync($"/api/account/{id}", payload);

            if (success) TempData["Success"] = "Cập nhật tài khoản thành công!";
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string oldPassword, string newPassword)
        {
            var payload = new { oldPassword, newPassword };
            var (success, message) = await _coreClient.PutAsync($"/api/account/{id}/change-password", payload);

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _coreClient.DeleteAsync($"/api/account/{id}");
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index");
        }
    }

    // =========================================================================
    // Audit Log Controller (Section 4.12 - Admin)
    // =========================================================================
    public class AuditLogController : Controller
    {
        private readonly CoreApiClient _coreClient;

        public AuditLogController(CoreApiClient coreClient)
        {
            _coreClient = coreClient;
        }

        public async Task<IActionResult> Index(string? user, string? entity, string? action, int page = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
                return RedirectToAction("Login", "Auth");

            if (HttpContext.Session.GetString("user_role") != "Admin")
            {
                TempData["Error"] = "Chỉ Quản trị viên (Admin) mới có quyền xem Lịch sử kiểm toán (Audit Log).";
                return RedirectToAction("Index", "News");
            }

            var queryParams = new List<string> { $"page={page}", "pageSize=15" };
            if (!string.IsNullOrWhiteSpace(user)) queryParams.Add($"user={Uri.EscapeDataString(user)}");
            if (!string.IsNullOrWhiteSpace(entity)) queryParams.Add($"entity={Uri.EscapeDataString(entity)}");
            if (!string.IsNullOrWhiteSpace(action)) queryParams.Add($"action={Uri.EscapeDataString(action)}");

            var endpoint = $"/api/auditlog?{string.Join("&", queryParams)}";
            var result = await _coreClient.GetAsync<PagedAuditLogResult>(endpoint) ?? new PagedAuditLogResult();

            ViewBag.User = user;
            ViewBag.Entity = entity;
            ViewBag.Action = action;

            return View(result);
        }
    }
}
