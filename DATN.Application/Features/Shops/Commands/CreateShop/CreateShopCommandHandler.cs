using DATN.Application.Common.Models;
using DATN.Domain.Entities.Shops;
using DATN.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.CreateShop;

public class CreateShopCommandHandler : IRequestHandler<CreateShopCommand, ApiResponse<Guid>>
{
    private readonly IShopRepository _shopRepository;

    public CreateShopCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateShopCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra Slug đã tồn tại hay chưa
        var existingShop = await _shopRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existingShop != null)
        {
            return ApiResponse<Guid>.Fail("Shop with the same Slug already exists.", 400);
        }

        // 2. Tạo Entity mới
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            CoverUrl = request.CoverUrl,
            OwnerId = request.OwnerId,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardId = request.WardId,
            PickupAddress = request.PickupAddress,
            IsActive = true, // Mặc định khi mới tạo là active
            Rating = 0,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Lưu vào DB
        await _shopRepository.AddAsync(shop, cancellationToken);

        return ApiResponse<Guid>.Succeed(shop.Id, "Shop created successfully.");
    }
}
