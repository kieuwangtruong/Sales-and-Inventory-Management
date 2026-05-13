namespace ReportService.DTOs;

// ─── Báo cáo doanh thu theo khoảng thời gian ─────────────────────────────────
public record RevenueReportRequest(DateTime From, DateTime To);

public record RevenueReportResponse(
    DateTime From,
    DateTime To,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AvgOrderValue,
    List<DailyRevenue> ByDay
);

public record DailyRevenue(DateTime Date, decimal Revenue, int Orders);

// ─── Thống kê tổng quan Dashboard ────────────────────────────────────────────
public record DashboardStats(
    decimal RevenueToday,
    decimal RevenueThisMonth,
    int OrdersToday,
    int TotalProducts,
    int LowStockProducts,
    List<TopProduct> TopProducts
);

public record TopProduct(int ProductId, string ProductName, int TotalSold, decimal Revenue);
