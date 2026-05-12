using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.DTOs;
using UserAuthService.Models;

namespace UserAuthService.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshAsync(string refreshToken, string expiredAccessToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(int userId);
}

public class AuthService(
    UserDbContext       db,
    IJwtTokenService    jwtService,
    IConfiguration      config) : IAuthService
{
    // ─── Đăng nhập ───────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        return await IssueTokenPairAsync(user);
    }

    // ─── Gia hạn token ───────────────────────────────────────────────────────
    // Nhận refresh token + access token cũ (expired OK) → trả về cặp mới
    public async Task<LoginResponse?> RefreshAsync(string refreshToken, string expiredAccessToken)
    {
        var principal = jwtService.ValidateExpiredToken(expiredAccessToken);
        if (principal is null) return null;

        var userId = int.Parse(principal.FindFirst("sub")?.Value ?? "0");

        var stored = await db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt =>
                rt.Token     == refreshToken &&
                rt.UserId    == userId       &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);

        if (stored is null) return null;

        // Rotate: thu hồi token cũ, cấp cặp mới
        var newPair = await IssueTokenPairAsync(stored.User);
        stored.IsRevoked       = true;
        stored.ReplacedByToken = newPair.RefreshToken;
        await db.SaveChangesAsync();

        return newPair;
    }

    // ─── Đăng xuất ───────────────────────────────────────────────────────────
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (token is null) return false;

        token.IsRevoked = true;
        await db.SaveChangesAsync();
        return true;
    }

    // ─── Thu hồi tất cả token của một user (Admin dùng khi lock account) ────
    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        tokens.ForEach(t => t.IsRevoked = true);
        await db.SaveChangesAsync();
    }

    // ─── Helper: tạo access + refresh token, lưu DB ─────────────────────────
    private async Task<LoginResponse> IssueTokenPairAsync(User user)
    {
        var accessToken      = jwtService.GenerateAccessToken(user);
        var refreshTokenStr  = jwtService.GenerateRefreshToken();
        var refreshExpiry    = int.Parse(config["Jwt:RefreshExpiryDays"] ?? "7");

        db.RefreshTokens.Add(new RefreshToken
        {
            Token     = refreshTokenStr,
            UserId    = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiry)
        });
        await db.SaveChangesAsync();

        var expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

        return new LoginResponse(
            AccessToken:  accessToken,
            RefreshToken: refreshTokenStr,
            ExpiresAt:    DateTime.UtcNow.AddMinutes(expiryMinutes),
            User: new UserInfo(user.Id, user.Username, user.FullName, user.Email, user.Role.Name, user.IsActive)
        );
    }
}
