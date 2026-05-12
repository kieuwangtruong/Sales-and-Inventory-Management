using Microsoft.EntityFrameworkCore;
using UserAuthService.Models;

namespace UserAuthService.Data;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User>         Users         => Set<User>();
    public DbSet<Role>         Roles         => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(100).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();

            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Role
        mb.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).HasMaxLength(20).IsRequired();
        });

        // RefreshToken
        mb.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed roles
        mb.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin",     Description = "Quản trị toàn hệ thống" },
            new Role { Id = 2, Name = "Sales",      Description = "Nhân viên bán hàng" },
            new Role { Id = 3, Name = "Warehouse",  Description = "Thủ kho" }
        );
    }
}

// ─── Seeder: tạo tài khoản admin mặc định khi DB trống ───────────────────────
public static class DbSeeder
{
    public static void Seed(UserDbContext db)
    {
        if (db.Users.Any()) return;

        db.Users.Add(new Models.User
        {
            Username     = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName     = "System Administrator",
            Email        = "admin@retail.local",
            RoleId       = 1,
            IsActive     = true
        });
        db.SaveChanges();
    }
}
