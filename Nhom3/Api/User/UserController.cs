using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nhom3.Application.DTOs;
using Nhom3.Application.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nhom3.Api.User
{
    [ApiController]
    [Route("api/[controller]")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllUsers()
        {
             try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Không tìm thấy User này!" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-username")]
        [Authorize]
        public async Task<IActionResult> GetUserByUserName([FromQuery] string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                    return BadRequest(new { success = false, message = "UserName không được để trống" });

                var user = await _userService.GetUserByUserNameAsync(userName);
                if (user == null)
                    return NotFound(new { success = false, message = "Không tìm thấy User này!" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-email")]
        [Authorize]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { success = false, message = "Email không được để trống" });

                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                    return NotFound(new { success = false, message = "Không tìm thấy User này!" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (createUserDto == null)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                var user = await _userService.CreateUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (updateUserDto == null)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                updateUserDto.Id = id;
                var user = await _userService.UpdateUserAsync(updateUserDto);
                return Ok(new { success = true, data = user });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequestDto loginRequestDto)
        {
            try
            {
                var loginResult = await _userService.LoginUserAsync(loginRequestDto);
                return Ok(new { success = true, data = loginResult });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto refreshRequestDto)
        {
            try
            {
                var refreshResult = await _userService.RefreshAccessTokenAsync(refreshRequestDto);
                return Ok(new { success = true, data = refreshResult });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto logoutRequestDto)
        {
            try
            {
                var accessToken = GetBearerToken();
                if (string.IsNullOrWhiteSpace(accessToken))
                    return Unauthorized(new { success = false, message = "Access token không hợp lệ" });

                await _userService.LogoutAsync(accessToken, logoutRequestDto ?? new LogoutRequestDto());
                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private string? GetBearerToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            const string prefix = "Bearer ";
            if (!authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            return authHeader.Substring(prefix.Length).Trim();
        }
    }
}