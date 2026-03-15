using DATN.Application.Common.Models;
using MediatR;
using System;

namespace DATN.Application.Features.Shops.Commands.CreateShop;

public class CreateShopCommand : IRequest<ApiResponse<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverUrl { get; set; }
    public Guid? OwnerId { get; set; }
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string? PickupAddress { get; set; }
}
