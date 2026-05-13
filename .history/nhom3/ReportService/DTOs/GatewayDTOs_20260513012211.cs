namespace ReportService.DTOs;

/// <summary>DTOs for responses from other microservices via Gateway</summary>

public record OrderSummaryDto(int Id, decimal TotalAmount, DateTime CreatedAt);

public record ProductStatsDto(int Total, int LowStock);

public record TopProductDto(int ProductId, string ProductName, int TotalSold, decimal Revenue);
