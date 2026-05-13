namespace ReportService.DTOs;

// ─── API Response wrapper ─────────────────────────────────────────────────────
public record ApiResponse<T>(bool Success, string? Message, T? Data);
public record ApiResponse(bool Success, string? Message);

// ─── DTO từ Order Service ─────────────────────────────────────────────────────
public record OrderSummaryDto(
    int     OrderId,
    DateTime CreatedAt,
    decimal TotalAmount,
    string  Status
);

public record ProductStatsDto(
    int     ProductId,
    string  ProductName,
    int     TotalSold,
    decimal Revenue
);
