using Nhom3.Domain.Entities;
using Nhom3.Domain.Interfaces;
using Nhom3.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom3.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUser _userRepository;

        public UserService(IUser userRepository)
        {
            _userRepository = userRepository;
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

        private static UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                LastModified = user.LastModified
            };
        }

    }
}