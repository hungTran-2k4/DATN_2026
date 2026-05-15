using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.UpdateShop;

public class UpdateShopCommandHandler : IRequestHandler<UpdateShopCommand, ApiResponse<bool>>
{
    private readonly IShopRepository _shopRepository;
    private readonly ICacheService _cache;

    public UpdateShopCommandHandler(IShopRepository shopRepository, ICacheService cache)
    {
        _shopRepository = shopRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateShopCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (shop == null)
        {
            return ApiResponse<bool>.Fail("Shop not found.", 404);
        }

        // Kiểm tra quyền sở hữu (chỉ cho phép owner sửa shop của họ, hoặc admin)
        // Trong context này, ta ép yêu cầu request.OwnerId phải khớp shop.OwnerId
        if (request.OwnerId.HasValue && shop.OwnerId != request.OwnerId.Value)
        {
            return ApiResponse<bool>.Fail("You do not have permission to update this shop.", 403);
        }

        // Tùy chọn: Kiểm tra xem trường hợp sửa Slug có trùng với Shop khác không
        var existingShop = await _shopRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existingShop != null && existingShop.Id != request.Id)
        {
            return ApiResponse<bool>.Fail("Another shop with the same Slug already exists.", 400);
        }

        // Cập nhật thông tin
        shop.Name = request.Name;
        shop.Slug = request.Slug;
        shop.Description = request.Description;
        shop.LogoUrl = request.LogoUrl;
        shop.CoverUrl = request.CoverUrl;
        shop.ProvinceId = request.ProvinceId;
        shop.DistrictId = request.DistrictId;
        shop.WardId = int.Parse(request.WardId);
        shop.PickupAddress = request.PickupAddress;
        
        if (request.IsActive.HasValue) 
        {
            shop.IsActive = request.IsActive;
        }

        var saveResult = await _shopRepository.UpdateAsync(shop, cancellationToken);

        if (!saveResult)
        {
            return ApiResponse<bool>.Fail("Failed to update shop.", 500);
        }

        _cache.RemoveByPrefix("shops:");
        return ApiResponse<bool>.Succeed(true, "Shop updated successfully.");
    }
}
