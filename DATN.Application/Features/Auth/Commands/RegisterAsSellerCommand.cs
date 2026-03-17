using DATN.Application.Common.Models;
using MediatR;

namespace DATN.Application.Features.Auth.Commands;

public class RegisterAsSellerCommand : IRequest<ApiResponse<Guid>>
{
    public Guid UserId { get; set; }

    // Shop info
    public string ShopName { get; set; } = string.Empty;
    public string ShopSlug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverUrl { get; set; }
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string? PickupAddress { get; set; }
}

