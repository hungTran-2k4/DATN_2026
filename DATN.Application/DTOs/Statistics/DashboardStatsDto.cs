using System;
using System.Collections.Generic;

namespace DATN.Application.DTOs.Statistics;

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalShops { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalSales { get; set; }
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<UserGrowthDto> UserGrowth { get; set; } = new();
    public List<OrderStatusDistributionDto> OrderStatusDistribution { get; set; } = new();
    public List<TopShopDto> TopShops { get; set; } = new();
}

public class SellerDashboardStatsDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal LockedBalance { get; set; }
    public int TotalProducts { get; set; }
    public double AverageRating { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<OrderStatusDistributionDto> OrderStatusSummary { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class UserGrowthDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OrderStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopShopDto
{
    public string ShopName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
}
