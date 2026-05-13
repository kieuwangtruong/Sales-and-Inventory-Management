namespace ReportService.Models;

// Snapshot lưu lại doanh thu từng ngày (được tính từ dữ liệu Order Service)
// Không lưu đơn hàng gốc — chỉ lưu số liệu đã tổng hợp
public class DailyRevenueSnapshot
{
    public int      Id          { get; set; }
    public DateTime Date        { get; set; }
    public decimal  TotalRevenue { get; set; }
    public int      TotalOrders  { get; set; }
    public decimal  AvgOrderValue { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
namespace ReportService.DTOs;

// ─── Báo cáo doanh thu theo khoảng thời gian ─────────────────────────────────
public record RevenueReportRequest(
    DateTime From,
    DateTime To
);

public record RevenueReportResponse(
    DateTime From,
    DateTime To,
    decimal  TotalRevenue,
    int      TotalOrders,
    decimal  AvgOrderValue,
    List<DailyRevenue> ByDay
);

public record DailyRevenue(
    DateTime Date,
    decimal  Revenue,
    int      Orders
);

// ─── Thống kê tổng quan Dashboard ────────────────────────────────────────────
public record DashboardStats(
    decimal RevenueToday,
    decimal RevenueThisMonth,
    int     OrdersToday,
    int     TotalProducts,
    int     LowStockProducts,     // số sản phẩm sắp hết (lấy từ Product Service)
    List<TopProduct> TopProducts  // top 5 sản phẩm bán chạy (lấy từ Order Service)
);

public record TopProduct(
    int     ProductId,            // reference ID — không FK cứng
    string  ProductName,
    int     TotalSold,
    decimal Revenue
);

// ─── Response wrapper ─────────────────────────────────────────────────────────
public record ApiResponse<T>(bool Success, string? Message, T? Data);
public record ApiResponse(bool Success, string? Message);
