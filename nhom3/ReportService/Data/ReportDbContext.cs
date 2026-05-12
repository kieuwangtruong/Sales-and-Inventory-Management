using Microsoft.EntityFrameworkCore;
using ReportService.Models;

namespace ReportService.Data;

public class ReportDbContext(DbContextOptions<ReportDbContext> options) : DbContext(options)
{
    public DbSet<DailyRevenueSnapshot> DailySnapshots => Set<DailyRevenueSnapshot>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DailyRevenueSnapshot>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Date).IsUnique();
            e.Property(s => s.TotalRevenue).HasPrecision(18, 2);
            e.Property(s => s.AvgOrderValue).HasPrecision(18, 2);
        });
    }
}
