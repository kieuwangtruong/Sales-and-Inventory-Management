using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportService.DTOs;
using ReportService.Services;

namespace ReportService.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]   // Toàn bộ Report API chỉ Admin
[Produces("application/json")]
public class ReportController(IReportService reportService) : ControllerBase
{
    // Lấy bearer token từ request để forward sang service khác
    private string BearerToken =>
        Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();

    /// <summary>Báo cáo doanh thu theo khoảng thời gian</summary>
    /// <param name="from">Từ ngày (yyyy-MM-dd)</param>
    /// <param name="to">Đến ngày (yyyy-MM-dd)</param>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueReportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new ApiResponse(false, "Ngày bắt đầu phải trước ngày kết thúc"));

        if ((to - from).TotalDays > 365)
            return BadRequest(new ApiResponse(false, "Khoảng thời gian tối đa là 365 ngày"));

        var report = await reportService.GetRevenueReportAsync(from, to, BearerToken);
        return Ok(new ApiResponse<RevenueReportResponse>(true, null, report));
    }

    /// <summary>Thống kê tổng quan Dashboard cho Admin</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStats>), 200)]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await reportService.GetDashboardStatsAsync(BearerToken);
        return Ok(new ApiResponse<DashboardStats>(true, null, stats));
    }
}
