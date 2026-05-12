using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserAuthService.Models;

namespace UserAuthService.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateExpiredToken(string token);
}

public class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    // ─── Access Token (JWT) ───────────────────────────────────────────────────
    // Chứa: userId, username, role → các service khác validate bằng shared secret
    public string GenerateAccessToken(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role,              user.Role.Name),
            new Claim("fullName",                   user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:            config["Jwt:Issuer"],
            claims:            claims,
            expires:           DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ─── Refresh Token (opaque random string) ────────────────────────────────
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    // ─── Dùng khi refresh: đọc claims từ expired token ───────────────────────
    public ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = false,
            ValidateLifetime         = false, // cho phép expired
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, parameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            return principal;
        }
        catch { return null; }
    }
}
