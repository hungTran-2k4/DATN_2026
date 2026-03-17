using DATN.Application.Common.Models;
using DATN.Application.Features.Auth.Commands;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Entities.Shops;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Auth.Handlers;

public class RegisterAsSellerHandler : IRequestHandler<RegisterAsSellerCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IShopRepository _shopRepo;

    public RegisterAsSellerHandler(IUserRepository userRepo, IRoleRepository roleRepo, IShopRepository shopRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _shopRepo = shopRepo;
    }

    public async Task<ApiResponse<Guid>> Handle(RegisterAsSellerCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShopName) || string.IsNullOrWhiteSpace(request.ShopSlug))
            return ApiResponse<Guid>.Fail("Thiếu thông tin shop (name/slug).", 400, "INVALID_SHOP_DATA");

        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<Guid>.Fail("Không tìm thấy người dùng.", 404, "USER_NOT_FOUND");

        // 1) Tạo shop nếu slug chưa tồn tại
        var existingShop = await _shopRepo.GetBySlugAsync(request.ShopSlug, cancellationToken);
        if (existingShop != null)
            return ApiResponse<Guid>.Fail("Slug shop đã tồn tại.", 400, "SHOP_SLUG_EXISTS");

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = request.ShopName.Trim(),
            Slug = request.ShopSlug.Trim().ToLowerInvariant(),
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            CoverUrl = request.CoverUrl,
            OwnerId = request.UserId,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardId = request.WardId,
            PickupAddress = request.PickupAddress,
            IsActive = true,
            Rating = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _shopRepo.AddAsync(shop, cancellationToken);

        // 2) Gán role Seller (tạo role nếu chưa có)
        const string SellerRoleName = "Seller";
        var sellerRole = await _roleRepo.GetByNameAsync(SellerRoleName, cancellationToken);
        if (sellerRole == null)
        {
            sellerRole = await _roleRepo.CreateAsync(new Role
            {
                Id = Guid.NewGuid(),
                Name = SellerRoleName,
                Description = "Seller role"
            }, cancellationToken);
        }

        await _userRepo.AssignRoleAsync(request.UserId, sellerRole.Id, cancellationToken);

        return ApiResponse<Guid>.Succeed(shop.Id, "Đăng ký bán hàng thành công.");
    }
}

