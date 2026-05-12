using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthService.DTOs;
using UserAuthService.Services;

namespace UserAuthService.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Đăng nhập — trả về access token + refresh token</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new ApiResponse(false, "Tên đăng nhập hoặc mật khẩu không đúng"));

        return Ok(new ApiResponse<LoginResponse>(true, "Đăng nhập thành công", result));
    }

    /// <summary>Gia hạn access token bằng refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request,
        [FromHeader(Name = "Authorization")] string? authHeader)
    {
        // Lấy access token cũ từ header (có thể expired)
        var expiredToken = authHeader?.Replace("Bearer ", "").Trim() ?? string.Empty;

        var result = await authService.RefreshAsync(request.RefreshToken, expiredToken);
        if (result is null)
            return Unauthorized(new ApiResponse(false, "Refresh token không hợp lệ hoặc đã hết hạn"));

        return Ok(new ApiResponse<LoginResponse>(true, "Gia hạn token thành công", result));
    }

    /// <summary>Đăng xuất — thu hồi refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse.Ok("Đăng xuất thành công"));
    }

    /// <summary>Lấy thông tin user đang đăng nhập từ token</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 200)]
    public IActionResult Me()
    {
        var userId   = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value ?? "0");
        var username = User.FindFirst(ClaimTypes.Name)?.Value
                       ?? User.FindFirst("unique_name")?.Value ?? "";
        var role     = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var fullName = User.FindFirst("fullName")?.Value ?? "";

        return Ok(new ApiResponse<UserInfo>(true, null,
            new UserInfo(userId, username, fullName, "", role, true)));
    }
}
