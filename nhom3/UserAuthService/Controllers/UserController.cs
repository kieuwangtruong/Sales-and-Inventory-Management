using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthService.DTOs;
using UserAuthService.Services;

namespace UserAuthService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UserController(IUserService userService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? "0");

    /// <summary>Danh sách tất cả user — Admin only</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<UserInfo>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllAsync();
        return Ok(new ApiResponse<List<UserInfo>>(true, null, users));
    }

    /// <summary>Chi tiết user theo Id — Admin hoặc chính user đó</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    public async Task<IActionResult> GetById(int id)
    {
        // Chỉ Admin hoặc chính user đó mới được xem
        if (!User.IsInRole("Admin") && CurrentUserId != id)
            return Forbid();

        var user = await userService.GetByIdAsync(id);
        if (user is null) return NotFound(new ApiResponse(false, "User không tồn tại"));

        return Ok(new ApiResponse<UserInfo>(true, null, user));
    }

    /// <summary>Tạo tài khoản mới — Admin only</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest dto)
    {
        var (ok, msg, user) = await userService.CreateAsync(dto);
        if (!ok) return BadRequest(new ApiResponse(false, msg));

        return CreatedAtAction(nameof(GetById), new { id = user!.Id },
            new ApiResponse<UserInfo>(true, msg, user));
    }

    /// <summary>Cập nhật thông tin user — Admin only</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest dto)
    {
        var (ok, msg) = await userService.UpdateAsync(id, dto);
        return ok ? Ok(ApiResponse.Ok(msg)) : BadRequest(new ApiResponse(false, msg));
    }

    /// <summary>Đổi mật khẩu — chỉ user tự đổi của mình</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest dto)
    {
        var (ok, msg) = await userService.ChangePasswordAsync(CurrentUserId, dto);
        return ok ? Ok(ApiResponse.Ok(msg)) : BadRequest(new ApiResponse(false, msg));
    }

    /// <summary>Xoá (vô hiệu hoá) user — Admin only</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId == id)
            return BadRequest(new ApiResponse(false, "Không thể tự xoá tài khoản của mình"));

        var (ok, msg) = await userService.DeleteAsync(id);
        return ok ? Ok(ApiResponse.Ok(msg)) : BadRequest(new ApiResponse(false, msg));
    }
}
