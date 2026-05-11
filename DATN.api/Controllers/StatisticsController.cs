using DATN.Application.Common.Models;
using DATN.Application.DTOs.Statistics;
using DATN.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Route("api/statistics")]
[ApiController]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStatsDto>>> GetAdminStats()
    {
        var stats = await _statisticsService.GetAdminDashboardStats();
        return Ok(new ApiResponse<AdminDashboardStatsDto>(stats));
    }

    [HttpGet("seller/{shopId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<ActionResult<ApiResponse<SellerDashboardStatsDto>>> GetSellerStats(Guid shopId)
    {
        // For security, you might want to verify if the current user owns this shop
        // But for now, we'll keep it simple
        var stats = await _statisticsService.GetSellerDashboardStats(shopId);
        return Ok(new ApiResponse<SellerDashboardStatsDto>(stats));
    }
}
