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
        private readonly ITokenBlacklist _tokenBlacklist;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiresMinutes;
        private readonly int _jwtRefreshDays;

        public UserService(IUser userRepository, ITokenBlacklist tokenBlacklist, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenBlacklist = tokenBlacklist;
            _jwtKey = configuration["Jwt:Key"] ?? string.Empty;
            _jwtIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
            _jwtAudience = configuration["Jwt:Audience"] ?? string.Empty;

            if (!int.TryParse(configuration["Jwt:ExpiresMinutes"], out _jwtExpiresMinutes))
                _jwtExpiresMinutes = 60;

            if (!int.TryParse(configuration["Jwt:RefreshDays"], out _jwtRefreshDays))
                _jwtRefreshDays = 7;
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
                RefreshToken = GenerateRefreshToken(user),
                User = MapToDto(user)
            };
        }

        public async Task<RefreshResponseDto> RefreshAccessTokenAsync(RefreshRequestDto refreshRequestDto)
        {
            if (refreshRequestDto == null || string.IsNullOrWhiteSpace(refreshRequestDto.RefreshToken))
                throw new ArgumentException("Refresh token không được để trống");

            var principal = ValidateRefreshToken(refreshRequestDto.RefreshToken);
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrWhiteSpace(jti))
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            if (await _tokenBlacklist.IsBlacklistedAsync(jti))
                throw new UnauthorizedAccessException("Refresh token đã bị vô hiệu");

            var userIdValue = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(userIdValue, out var userId))
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");

            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy User");

            return new RefreshResponseDto
            {
                AccessToken = GenerateAccessToken(user)
            };
        }

        public async Task LogoutAsync(string accessToken, LogoutRequestDto logoutRequestDto)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token không được để trống");

            var accessInfo = ReadTokenInfo(accessToken);
            if (accessInfo.TokenType != "access")
                throw new ArgumentException("Access token không hợp lệ");

            await AddToBlacklistAsync(accessInfo, logoutRequestDto?.DeviceId);

            if (!string.IsNullOrWhiteSpace(logoutRequestDto?.RefreshToken))
            {
                var refreshInfo = ReadTokenInfo(logoutRequestDto.RefreshToken);
                if (refreshInfo.TokenType != "refresh")
                    throw new ArgumentException("Refresh token không hợp lệ");

                await AddToBlacklistAsync(refreshInfo, logoutRequestDto.DeviceId);
            }
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
                new Claim("typ", "access"),
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

        private string GenerateRefreshToken(User user)
        {
            if (string.IsNullOrWhiteSpace(_jwtKey))
                throw new InvalidOperationException("Jwt:Key is missing");

            if (string.IsNullOrWhiteSpace(_jwtIssuer))
                throw new InvalidOperationException("Jwt:Issuer is missing");

            if (string.IsNullOrWhiteSpace(_jwtAudience))
                throw new InvalidOperationException("Jwt:Audience is missing");

            var claims = new List<Claim>
            {
                new Claim("typ", "refresh"),
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
                expires: DateTime.UtcNow.AddDays(_jwtRefreshDays),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtIssuer,
                    ValidAudience = _jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                var principal = tokenHandler.ValidateToken(refreshToken, parameters, out _);
                var tokenType = principal.FindFirst("typ")?.Value;

                if (tokenType != "refresh")
                    throw new UnauthorizedAccessException("Refresh token không hợp lệ");

                return principal;
            }
            catch (SecurityTokenException)
            {
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");
            }
        }

        private static TokenInfo ReadTokenInfo(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var jti = jwt.Id;
            if (string.IsNullOrWhiteSpace(jti))
                jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

            var tokenType = jwt.Claims.FirstOrDefault(c => c.Type == "typ")?.Value ?? string.Empty;
            int? userId = null;

            if (int.TryParse(jwt.Subject, out var parsedUserId))
                userId = parsedUserId;

            return new TokenInfo(jti, tokenType, jwt.ValidTo, userId);
        }

        private async Task AddToBlacklistAsync(TokenInfo tokenInfo, string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(tokenInfo.Jti))
                return;

            var token = new BlacklistedToken
            {
                UserId = tokenInfo.UserId,
                Jti = tokenInfo.Jti,
                TokenType = tokenInfo.TokenType,
                DeviceId = deviceId,
                ExpiresAt = tokenInfo.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _tokenBlacklist.AddAsync(token);
        }

        private sealed record TokenInfo(string Jti, string TokenType, DateTime ExpiresAt, int? UserId);
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