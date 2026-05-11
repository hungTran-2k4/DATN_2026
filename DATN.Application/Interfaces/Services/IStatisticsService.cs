using DATN.Application.DTOs.Statistics;
using System;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces.Services;

public interface IStatisticsService
{
    Task<AdminDashboardStatsDto> GetAdminDashboardStats();
    Task<SellerDashboardStatsDto> GetSellerDashboardStats(Guid shopId);
}
