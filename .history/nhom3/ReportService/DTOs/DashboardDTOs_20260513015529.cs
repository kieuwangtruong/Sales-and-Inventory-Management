namespace ReportService.DTOs;

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
