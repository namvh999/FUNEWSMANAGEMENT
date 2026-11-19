using System.Security.Claims;
using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.DTOs;
using FUNewsManagement_CoreAPI.Models;
using FUNewsManagement_CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FUNewsDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _config;

        public AuthController(FUNewsDbContext context, ITokenService tokenService, IAuditService auditService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _auditService = auditService;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = await _context.SystemAccounts
                .FirstOrDefaultAsync(a => a.AccountEmail == dto.Email && a.AccountPassword == dto.Password);

            if (account == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
            }

            var accessToken = _tokenService.GenerateAccessToken(account);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshExpiryDays = int.TryParse(_config["Jwt:RefreshTokenExpiryDays"], out int days) ? days : 7;
            account.RefreshToken = refreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshExpiryDays);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(account.AccountID.ToString(), account.AccountName, "LOGIN", "SystemAccount", account.AccountID.ToString(), null, new { LoginTime = DateTime.UtcNow });

            var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out int exp) ? exp : 60;

            return Ok(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(expiryMinutes),
                AccountId = account.AccountID,
                AccountName = account.AccountName,
                AccountEmail = account.AccountEmail,
                Role = account.AccountRole == 1 ? "Admin" : "Staff"
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            if (principal == null)
                return BadRequest(new { message = "Access token không hợp lệ." });

            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var account = await _context.SystemAccounts.FirstOrDefaultAsync(a => a.AccountEmail == emailClaim);

            if (account == null || account.RefreshToken != dto.RefreshToken || account.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Refresh token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại." });
            }

            var newAccessToken = _tokenService.GenerateAccessToken(account);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var refreshExpiryDays = int.TryParse(_config["Jwt:RefreshTokenExpiryDays"], out int days) ? days : 7;
            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshExpiryDays);
            await _context.SaveChangesAsync();

            var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out int exp) ? exp : 60;

            return Ok(new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(expiryMinutes),
                AccountId = account.AccountID,
                AccountName = account.AccountName,
                AccountEmail = account.AccountEmail,
                Role = account.AccountRole == 1 ? "Admin" : "Staff"
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var account = await _context.SystemAccounts.FindAsync(userId);
            if (account == null)
                return NotFound();

            return Ok(new AccountDto
            {
                AccountID = account.AccountID,
                AccountName = account.AccountName,
                AccountEmail = account.AccountEmail,
                AccountRole = account.AccountRole
            });
        }
    }
}
