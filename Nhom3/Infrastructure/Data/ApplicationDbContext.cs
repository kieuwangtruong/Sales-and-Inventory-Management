using Microsoft.EntityFrameworkCore;
using Nhom3.Domain.Entities;

namespace Nhom3.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Cấu hình quan hệ giữa Order và OrderItem
            modelBuilder.Entity<User>()
                .ToTable("Users")
                .HasKey(u => u.Id)
                .HasIndex(u => u.Email).IsUnique();
        }
    }
}