using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nhom3.Application.DTOs;
using Nhom3.Domain.Entities;
using Nhom3.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Nhom3.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUser _userRepository;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiresMinutes;

        public UserService(IUser userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtKey = configuration["Jwt:Key"] ?? string.Empty;
            _jwtIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
            _jwtAudience = configuration["Jwt:Audience"] ?? string.Empty;

            if (!int.TryParse(configuration["Jwt:ExpiresMinutes"], out _jwtExpiresMinutes))
                _jwtExpiresMinutes = 60;
        }

        // Lấy tất cả user
        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsers();
            return users.Select(MapToDto).ToList();
        }

        // lấy user theo id
        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserById(id);
            return user != null ? MapToDto(user) : null;
        }

        // lấy user theo userName
        public async Task<UserResponseDto?> GetUserByUserNameAsync(string userName)
        {
            var user = await _userRepository.GetUserByUserName(userName);
            return user != null ? MapToDto(user) : null;
        }

        // lấy user theo email
        public async Task<UserResponseDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmail(email);
            return user != null ? MapToDto(user) : null;
        }

        // Tạo user mới
        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            if (string.IsNullOrWhiteSpace(createUserDto.UserName))
                throw new ArgumentException("UserName không được để trống");

            if (string.IsNullOrWhiteSpace(createUserDto.Email))
                throw new ArgumentException("Email không được để trống");

            if (string.IsNullOrWhiteSpace(createUserDto.PasswordHash))
                throw new ArgumentException("Password không được để trống");

            var existingByEmail = await _userRepository.GetUserByEmail(createUserDto.Email);
            if (existingByEmail != null)
                throw new InvalidOperationException("Email đã tồn tại");

            var existingByUserName = await _userRepository.GetUserByUserName(createUserDto.UserName);
            if (existingByUserName != null)
                throw new InvalidOperationException("UserName đã tồn tại");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.PasswordHash);

            var user = new User(
                createUserDto.UserName,
                createUserDto.FullName,
                createUserDto.Email,
                passwordHash,
                createUserDto.DateOfBirth,
                createUserDto.Sex,
                createUserDto.Address
            );

            await _userRepository.AddUser(user);

            return MapToDto(user);
        }

        // Cập nhật user
        public async Task<UserResponseDto> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetUserById(updateUserDto.Id);

            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy User với ID {updateUserDto.Id}");

            if (!string.IsNullOrWhiteSpace(updateUserDto.UserName))
                user.UserName = updateUserDto.UserName;

            if (!string.IsNullOrWhiteSpace(updateUserDto.FullName))
                user.FullName = updateUserDto.FullName;

            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
                user.Email = updateUserDto.Email;

            if (!string.IsNullOrWhiteSpace(updateUserDto.PasswordHash))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.PasswordHash);

            if (updateUserDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateUserDto.DateOfBirth.Value;

            if (updateUserDto.Sex.HasValue)
                user.Sex = updateUserDto.Sex.Value;

            if (!string.IsNullOrWhiteSpace(updateUserDto.Address))
                user.Address = updateUserDto.Address;

            user.LastModified = DateTime.Now;

            await _userRepository.UpdateUser(user);

            return MapToDto(user);
        }

        // Xóa user
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetUserById(id);

            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy User với ID {id}");

            await _userRepository.DeleteUser(id);
            return true;
        }
        
        public async Task<LoginResponseDto> LoginUserAsync(LoginRequestDto loginRequestDto)
        {
            if (loginRequestDto == null)
                throw new ArgumentException("Dữ liệu đăng nhập không hợp lệ");

            if (string.IsNullOrWhiteSpace(loginRequestDto.Email))
                throw new ArgumentException("Email không được để trống");

            if (string.IsNullOrWhiteSpace(loginRequestDto.Password))
                throw new ArgumentException("Password không được để trống");

            var user = await _userRepository.GetUserByEmail(loginRequestDto.Email);

            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy User với Email {loginRequestDto.Email}");

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Mật khẩu không đúng");

            return new LoginResponseDto
            {
                AccessToken = GenerateAccessToken(user),
                User = MapToDto(user)
            };
        }

        private string GenerateAccessToken(User user)
        {
            if (string.IsNullOrWhiteSpace(_jwtKey))
                throw new InvalidOperationException("Jwt:Key is missing");

            if (string.IsNullOrWhiteSpace(_jwtIssuer))
                throw new InvalidOperationException("Jwt:Issuer is missing");

            if (string.IsNullOrWhiteSpace(_jwtAudience))
                throw new InvalidOperationException("Jwt:Audience is missing");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private static UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                LastModified = user.LastModified
            };
        }

        

    }
}