using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.DTOs;
using UserAuthService.Models;

namespace UserAuthService.Services;

public interface IUserService
{
    Task<List<UserInfo>> GetAllAsync();
    Task<UserInfo?> GetByIdAsync(int id);
    Task<(bool ok, string msg, UserInfo? user)> CreateAsync(CreateUserRequest dto);
    Task<(bool ok, string msg)> UpdateAsync(int id, UpdateUserRequest dto);
    Task<(bool ok, string msg)> ChangePasswordAsync(int callerId, ChangePasswordRequest dto);
    Task<(bool ok, string msg)> DeleteAsync(int id);
}

public class UserService(UserDbContext db, IAuthService authService) : IUserService
{
    // ─── Lấy danh sách tất cả user (Admin only) ───────────────────────────
    public async Task<List<UserInfo>> GetAllAsync()
    {
        return await db.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .Select(u => new UserInfo(u.Id, u.Username, u.FullName, u.Email, u.Role.Name, u.IsActive))
            .ToListAsync();
    }

    // ─── Lấy thông tin một user ───────────────────────────────────────────
    public async Task<UserInfo?> GetByIdAsync(int id)
    {
        var u = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (u is null) return null;
        return new UserInfo(u.Id, u.Username, u.FullName, u.Email, u.Role.Name, u.IsActive);
    }

    // ─── Tạo user mới (Admin only) ────────────────────────────────────────
    public async Task<(bool ok, string msg, UserInfo? user)> CreateAsync(CreateUserRequest dto)
    {
        if (await db.Users.AnyAsync(u => u.Username == dto.Username))
            return (false, "Username đã tồn tại", null);

        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return (false, "Email đã được sử dụng", null);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
        if (role is null)
            return (false, $"Role '{dto.Role}' không hợp lệ. Dùng: Admin, Sales, Warehouse", null);

        var user = new User
        {
            Username     = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName     = dto.FullName,
            Email        = dto.Email,
            RoleId       = role.Id
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (true, "Tạo tài khoản thành công",
            new UserInfo(user.Id, user.Username, user.FullName, user.Email, role.Name, user.IsActive));
    }

    // ─── Cập nhật thông tin user (Admin only) ────────────────────────────
    public async Task<(bool ok, string msg)> UpdateAsync(int id, UpdateUserRequest dto)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return (false, "User không tồn tại");

        if (dto.FullName  is not null) user.FullName  = dto.FullName;
        if (dto.Email     is not null) user.Email      = dto.Email;
        if (dto.IsActive  is not null)
        {
            user.IsActive = dto.IsActive.Value;
            // Nếu bị vô hiệu hoá → thu hồi toàn bộ token
            if (!dto.IsActive.Value)
                await authService.RevokeAllUserTokensAsync(id);
        }
        if (dto.Role is not null)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
            if (role is null) return (false, $"Role '{dto.Role}' không hợp lệ");
            user.RoleId = role.Id;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, "Cập nhật thành công");
    }

    // ─── Đổi mật khẩu (tự đổi) ───────────────────────────────────────────
    public async Task<(bool ok, string msg)> ChangePasswordAsync(int callerId, ChangePasswordRequest dto)
    {
        var user = await db.Users.FindAsync(callerId);
        if (user is null) return (false, "User không tồn tại");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return (false, "Mật khẩu hiện tại không đúng");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt    = DateTime.UtcNow;

        // Thu hồi tất cả refresh token sau khi đổi mật khẩu (bảo mật)
        await authService.RevokeAllUserTokensAsync(callerId);

        await db.SaveChangesAsync();
        return (true, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại");
    }

    // ─── Xoá user (soft delete, Admin only) ──────────────────────────────
    public async Task<(bool ok, string msg)> DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return (false, "User không tồn tại");

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await authService.RevokeAllUserTokensAsync(id);
        await db.SaveChangesAsync();

        return (true, "Đã vô hiệu hoá tài khoản");
    }
}
