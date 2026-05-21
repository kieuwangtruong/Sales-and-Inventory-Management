using Microsoft.EntityFrameworkCore;
using Nhom3.Domain.Entities;

namespace Nhom3.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue(User.UserRole.User);
            });

            modelBuilder.Entity<BlacklistedToken>(entity =>
            {
                entity.ToTable("BlacklistedTokens");
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.Jti).IsUnique();
                entity.HasIndex(t => t.ExpiresAt);
            });
        }
    }
}