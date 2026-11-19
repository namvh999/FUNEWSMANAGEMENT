using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FUNewsManagement_CoreAPI.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userName, string action, string entity, string? entityId, object? beforeData, object? afterData);
    }

    public class AuditService : IAuditService
    {
        private readonly FUNewsDbContext _context;

        public AuditService(FUNewsDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string? userId, string? userName, string action, string entity, string? entityId, object? beforeData, object? afterData)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId ?? "System",
                    UserName = userName ?? "System",
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    BeforeData = beforeData != null ? JsonSerializer.Serialize(beforeData) : null,
                    AfterData = afterData != null ? JsonSerializer.Serialize(afterData) : null,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Ensure audit logging failures do not break primary operations
            }
        }
    }

    public interface ITokenService
    {
        string GenerateAccessToken(SystemAccount account);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(SystemAccount account)
        {
            var keyString = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountID.ToString()),
                new Claim(ClaimTypes.Name, account.AccountName),
                new Claim(ClaimTypes.Email, account.AccountEmail),
                new Claim(ClaimTypes.Role, account.AccountRole == 1 ? "Admin" : "Staff")
            };

            var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out int exp) ? exp : 60;

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "FUNewsManagement",
                audience: _config["Jwt:Audience"] ?? "FUNewsManagementClients",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var keyString = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                ValidateLifetime = false // Here we don't care about token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
    }
}
