namespace ReportService.Models;

/// <summary>
/// Snapshot lưu lại doanh thu từng ngày (được tính từ dữ liệu Order Service)
/// Không lưu đơn hàng gốc — chỉ lưu số liệu đã tổng hợp
/// </summary>
public class DailyRevenueSnapshot
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
