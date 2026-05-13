namespace ReportService.DTOs;

/// <summary>Generic API response wrapper</summary>
public record ApiResponse<T>(bool Success, string? Message, T? Data);

/// <summary>API response wrapper without data</summary>
public record ApiResponse(bool Success, string? Message);
