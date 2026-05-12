using System.ComponentModel.DataAnnotations;

namespace UserAuthService.DTOs;

// ─── Auth DTOs ────────────────────────────────────────────────────────────────
public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

public record RefreshTokenRequest(
    [Required] string RefreshToken
);

public record LogoutRequest(
    [Required] string RefreshToken
);

// ─── User DTOs ────────────────────────────────────────────────────────────────
public record UserInfo(
    int    Id,
    string Username,
    string FullName,
    string Email,
    string Role,
    bool   IsActive
);

public record CreateUserRequest(
    [Required][MinLength(3)] string Username,
    [Required][MinLength(6)] string Password,
    [Required]               string FullName,
    [Required][EmailAddress] string Email,
    [Required]               string Role      // "Admin" | "Sales" | "Warehouse"
);

public record UpdateUserRequest(
    string? FullName,
    string? Email,
    string? Role,
    bool?   IsActive
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(6)] string NewPassword
);

// ─── Generic response ────────────────────────────────────────────────────────
public record ApiResponse<T>(bool Success, string? Message, T? Data);
public record ApiResponse(bool Success, string? Message)
{
    public static ApiResponse Ok(string? msg = null)    => new(true,  msg);
    public static ApiResponse Fail(string msg)          => new(false, msg);
}
