using Nhom3.Application.DTOs;

namespace Nhom3.Application.Services
{
    public interface IUserService
    {
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto?> GetUserByUserNameAsync(string userName);
        Task<UserResponseDto?> GetUserByEmailAsync(string email);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserResponseDto> UpdateUserAsync(UpdateUserDto updateUserDto);
        Task<LoginResponseDto> LoginUserAsync(LoginRequestDto loginRequestDto);
        Task<RefreshResponseDto> RefreshAccessTokenAsync(RefreshRequestDto refreshRequestDto);
        Task LogoutAsync(string accessToken, LogoutRequestDto logoutRequestDto);
        Task<bool> DeleteUserAsync(int id);
    }
}