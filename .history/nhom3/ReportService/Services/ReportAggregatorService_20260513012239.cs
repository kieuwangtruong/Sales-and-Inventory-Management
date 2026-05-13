using Microsoft.EntityFrameworkCore;
using ReportService.Data;
using ReportService.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ReportService.Services;

public interface IReportService
{
    Task<RevenueReportResponse> GetRevenueReportAsync(DateTime from, DateTime to, string bearerToken);
    Task<DashboardStats> GetDashboardStatsAsync(string bearerToken);
}

public class ReportAggregatorService(
    ReportDbContext    db,
    IHttpClientFactory httpFactory,
    ILogger<ReportAggregatorService> logger) : IReportService
{
    // ─── Báo cáo doanh thu theo khoảng ngày ──────────────────────────────────
    // Gọi Order Service để lấy đơn hàng, tổng hợp tại đây
    public async Task<RevenueReportResponse> GetRevenueReportAsync(
        DateTime from, DateTime to, string bearerToken)
    {
        // Gọi Order Service qua Gateway
        var orders = await FetchFromGatewayAsync<List<OrderSummaryDto>>(
            $"/api/orders/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            bearerToken);

        if (orders is null || orders.Count == 0)
        {
            return new RevenueReportResponse(from, to, 0, 0, 0, new List<DailyRevenue>());
        }

        var byDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailyRevenue(
                Date:    g.Key,
                Revenue: g.Sum(o => o.TotalAmount),
                Orders:  g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var totalRevenue = byDay.Sum(d => d.Revenue);
        var totalOrders  = byDay.Sum(d => d.Orders);

        return new RevenueReportResponse(
            From:          from,
            To:            to,
            TotalRevenue:  totalRevenue,
            TotalOrders:   totalOrders,
            AvgOrderValue: totalOrders > 0 ? totalRevenue / totalOrders : 0,
            ByDay:         byDay
        );
    }

    // ─── Dashboard stats (tổng hợp từ nhiều service) ─────────────────────────
    public async Task<DashboardStats> GetDashboardStatsAsync(string bearerToken)
    {
        var today     = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Gọi song song các service để giảm latency
        var ordersToday   = FetchFromGatewayAsync<List<OrderSummaryDto>>(
            $"/api/orders/summary?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}", bearerToken);
        var ordersMonth   = FetchFromGatewayAsync<List<OrderSummaryDto>>(
            $"/api/orders/summary?from={monthStart:yyyy-MM-dd}&to={today:yyyy-MM-dd}", bearerToken);
        var productStats  = FetchFromGatewayAsync<ProductStatsDto>(
            "/api/products/stats", bearerToken);
        var topProducts   = FetchFromGatewayAsync<List<TopProductDto>>(
            $"/api/orders/top-products?from={monthStart:yyyy-MM-dd}&to={today:yyyy-MM-dd}&take=5", bearerToken);

        await Task.WhenAll(ordersToday, ordersMonth, productStats, topProducts);

        var todayData  = await ordersToday  ?? new List<OrderSummaryDto>();
        var monthData  = await ordersMonth  ?? new List<OrderSummaryDto>();
        var prodData   = await productStats ?? new ProductStatsDto(0, 0);
        var topData    = await topProducts  ?? new List<TopProductDto>();

        return new DashboardStats(
            RevenueToday:     todayData.Sum(o => o.TotalAmount),
            RevenueThisMonth: monthData.Sum(o => o.TotalAmount),
            OrdersToday:      todayData.Count,
            TotalProducts:    prodData.Total,
            LowStockProducts: prodData.LowStock,
            TopProducts:      topData.Select(t => new TopProduct(
                t.ProductId, t.ProductName, t.TotalSold, t.Revenue)).ToList()
        );
    }

    // ─── Helper: gọi Gateway với JWT Bearer token ─────────────────────────────
    // Nếu service bị down → log lỗi và trả null (không crash toàn bộ report)
    private async Task<T?> FetchFromGatewayAsync<T>(string path, string bearerToken)
    {
        try
        {
            var client = httpFactory.CreateClient("gateway");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await client.GetAsync(path);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gateway call failed: {Path} — {Status}", path, response.StatusCode);
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex)
        {
            // Service khác down → không làm crash Report Service
            logger.LogError("Service unavailable at {Path}: {Message}", path, ex.Message);
            return default;
        }
    }
}
