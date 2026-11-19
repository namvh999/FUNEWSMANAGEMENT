using System.Text;
using System.Text.Json;
using FUNewsManagement_FE.Models;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_FE.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IHttpClientFactory clientFactory, ILogger<AuthController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("access_token")))
            {
                var role = HttpContext.Session.GetString("user_role");
                return role == "Admin" ? RedirectToAction("Index", "Dashboard") : RedirectToAction("Index", "News");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                return View(model);
            }

            try
            {
                var client = _clientFactory.CreateClient("CoreApi");
                var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var resJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resJson);
                    var root = doc.RootElement;

                    var accessToken = root.GetProperty("accessToken").GetString() ?? "";
                    var refreshToken = root.GetProperty("refreshToken").GetString() ?? "";
                    var role = root.GetProperty("role").GetString() ?? "Staff";
                    var accountName = root.GetProperty("accountName").GetString() ?? "";
                    var accountEmail = root.GetProperty("accountEmail").GetString() ?? "";
                    var accountId = root.GetProperty("accountId").GetInt32();

                    // Save session
                    HttpContext.Session.SetString("access_token", accessToken);
                    HttpContext.Session.SetString("refresh_token", refreshToken);
                    HttpContext.Session.SetString("user_role", role);
                    HttpContext.Session.SetString("user_name", accountName);
                    HttpContext.Session.SetString("user_email", accountEmail);
                    HttpContext.Session.SetInt32("user_id", accountId);

                    TempData["Success"] = $"Đăng nhập thành công! Chào mừng {accountName} ({role}).";

                    if (role == "Admin")
                        return RedirectToAction("Index", "Dashboard");

                    return RedirectToAction("Index", "News");
                }
                else
                {
                    var errString = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errString);
                        ViewBag.Error = doc.RootElement.GetProperty("message").GetString() ?? "Đăng nhập thất bại.";
                    }
                    catch
                    {
                        ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login exception");
                ViewBag.Error = "Không thể kết nối đến máy chủ xác thực Core API. Vui lòng kiểm tra lại trạng thái API.";
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Bạn đã đăng xuất khỏi hệ thống.";
            return RedirectToAction("Login");
        }
    }
}
