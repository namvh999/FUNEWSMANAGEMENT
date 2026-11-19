using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.DTOs;
using FUNewsManagement_CoreAPI.Models;
using FUNewsManagement_CoreAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FUNewsManagement_CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly FUNewsDbContext _context;
        private readonly IAuditService _auditService;

        public AccountController(FUNewsDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /api/account
        // Filter accounts by role (Admin / Staff)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? role)
        {
            var query = _context.SystemAccounts
                .Include(a => a.CreatedArticles)
                .AsQueryable();

            if (role.HasValue && (role.Value == 1 || role.Value == 2))
            {
                query = query.Where(a => a.AccountRole == role.Value);
            }

            var accounts = await query
                .OrderBy(a => a.AccountRole)
                .ThenBy(a => a.AccountName)
                .Select(a => new AccountDto
                {
                    AccountID = a.AccountID,
                    AccountName = a.AccountName,
                    AccountEmail = a.AccountEmail,
                    AccountRole = a.AccountRole,
                    CreatedArticleCount = a.CreatedArticles.Count
                })
                .ToListAsync();

            return Ok(accounts);
        }

        // GET: /api/account/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var account = await _context.SystemAccounts
                .Include(a => a.CreatedArticles)
                .FirstOrDefaultAsync(a => a.AccountID == id);

            if (account == null)
                return NotFound(new { message = $"Không tìm thấy tài khoản #{id}." });

            return Ok(new AccountDto
            {
                AccountID = account.AccountID,
                AccountName = account.AccountName,
                AccountEmail = account.AccountEmail,
                AccountRole = account.AccountRole,
                CreatedArticleCount = account.CreatedArticles.Count
            });
        }

        // POST: /api/account
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.AccountEmail.Trim().ToLower();
            if (await _context.SystemAccounts.AnyAsync(a => a.AccountEmail.ToLower() == email))
            {
                return BadRequest(new { message = $"Email '{email}' đã tồn tại trong hệ thống." });
            }

            var account = new SystemAccount
            {
                AccountName = dto.AccountName.Trim(),
                AccountEmail = email,
                AccountRole = dto.AccountRole,
                AccountPassword = dto.AccountPassword
            };

            _context.SystemAccounts.Add(account);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "CREATE",
                "SystemAccount",
                account.AccountID.ToString(),
                null,
                new { account.AccountName, account.AccountEmail, account.AccountRole }
            );

            return CreatedAtAction(nameof(GetById), new { id = account.AccountID }, new { id = account.AccountID, message = "Tạo tài khoản thành công!" });
        }

        // PUT: /api/account/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = await _context.SystemAccounts.FindAsync(id);
            if (account == null)
                return NotFound(new { message = $"Không tìm thấy tài khoản #{id}." });

            var email = dto.AccountEmail.Trim().ToLower();
            if (await _context.SystemAccounts.AnyAsync(a => a.AccountID != id && a.AccountEmail.ToLower() == email))
            {
                return BadRequest(new { message = $"Email '{email}' đã được sử dụng bởi tài khoản khác." });
            }

            var beforeData = new { account.AccountName, account.AccountEmail, account.AccountRole };

            account.AccountName = dto.AccountName.Trim();
            account.AccountEmail = email;
            account.AccountRole = dto.AccountRole;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "UPDATE",
                "SystemAccount",
                id.ToString(),
                beforeData,
                new { account.AccountName, account.AccountEmail, account.AccountRole }
            );

            return Ok(new { message = "Cập nhật tài khoản thành công!", id = account.AccountID });
        }

        // PUT: /api/account/{id}/change-password
        // Enforce Business Rule: Require old password for password changes!
        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = await _context.SystemAccounts.FindAsync(id);
            if (account == null)
                return NotFound(new { message = $"Không tìm thấy tài khoản #{id}." });

            // Validate old password
            if (account.AccountPassword != dto.OldPassword)
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác. Vui lòng thử lại." });
            }

            account.AccountPassword = dto.NewPassword;
            account.RefreshToken = null; // Invalidate refresh token on password change
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "UPDATE_PASSWORD",
                "SystemAccount",
                id.ToString(),
                null,
                new { message = "Password updated" }
            );

            return Ok(new { message = "Đổi mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới." });
        }

        // DELETE: /api/account/{id}
        // Enforce Business Rule: Do not delete accounts that created articles!
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var account = await _context.SystemAccounts
                .Include(a => a.CreatedArticles)
                .FirstOrDefaultAsync(a => a.AccountID == id);

            if (account == null)
                return NotFound(new { message = $"Không tìm thấy tài khoản #{id}." });

            if (account.CreatedArticles.Any())
            {
                return BadRequest(new
                {
                    message = $"Không thể xóa tài khoản '{account.AccountName}' ({account.AccountEmail}) vì tài khoản này đã tạo {account.CreatedArticles.Count} bài viết trong hệ thống."
                });
            }

            var beforeData = new { account.AccountName, account.AccountEmail, account.AccountRole };

            _context.SystemAccounts.Remove(account);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "DELETE",
                "SystemAccount",
                id.ToString(),
                beforeData,
                null
            );

            return Ok(new { message = $"Đã xóa tài khoản '{account.AccountName}' thành công." });
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        private string? GetCurrentUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    }
}
