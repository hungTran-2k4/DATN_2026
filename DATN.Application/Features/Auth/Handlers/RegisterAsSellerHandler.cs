using DATN.Application.Common.Models;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Entities.Shops;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Auth.Handlers;

public class RegisterAsSellerHandler : IRequestHandler<RegisterAsSellerCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IShopRepository _shopRepo;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _uow;

    public RegisterAsSellerHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IShopRepository shopRepo,
        ICacheService cache,
        IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _shopRepo = shopRepo;
        _cache = cache;
        _uow = uow;
    }

    public async Task<ApiResponse<Guid>> Handle(RegisterAsSellerCommand request, CancellationToken cancellationToken)
    {
        // 1) Validate input
        if (string.IsNullOrWhiteSpace(request.ShopName) || string.IsNullOrWhiteSpace(request.ShopSlug))
            return ApiResponse<Guid>.Fail("Thiếu thông tin shop (name/slug).", 400, "INVALID_SHOP_DATA");

        // Validate slug format: chỉ chứa chữ thường, số, dấu gạch ngang
        var normalizedSlug = request.ShopSlug.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedSlug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return ApiResponse<Guid>.Fail("Slug không hợp lệ. Chỉ được chứa chữ thường, số và dấu gạch ngang.", 400, "INVALID_SLUG_FORMAT");

        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<Guid>.Fail("Không tìm thấy người dùng.", 404, "USER_NOT_FOUND");

        // 2) Kiểm tra slug unique
        var existingShop = await _shopRepo.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (existingShop != null)
            return ApiResponse<Guid>.Fail("Slug shop đã tồn tại.", 400, "SHOP_SLUG_EXISTS");

        // 3) Kiểm tra user chưa có shop active/pending (tránh đăng ký trùng)
        var existingUserShops = await _shopRepo.GetByOwnerIdAsync(request.UserId, cancellationToken);
        var activeShop = existingUserShops.FirstOrDefault(s =>
            s.ApprovalStatus == ShopApprovalStatus.Pending ||
            s.ApprovalStatus == ShopApprovalStatus.Approved);
        if (activeShop != null)
            return ApiResponse<Guid>.Fail("Bạn đã có shop đang hoạt động hoặc đang chờ duyệt.", 400, "SHOP_ALREADY_EXISTS");

        // 4) Lấy/tạo role Seller trước transaction
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

        // 5) Tạo shop và gán role trong transaction (atomic)
        var shopId = Guid.NewGuid();
        using var tx = _uow.BeginTransaction();
        try
        {
            var shop = new Shop
            {
                Id = shopId,
                Name = request.ShopName.Trim(),
                Slug = normalizedSlug,
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
                ApprovalStatus = ShopApprovalStatus.Pending, // Tường minh
                CreatedAt = DateTime.UtcNow
            };

            await _shopRepo.AddAsync(shop, cancellationToken);
            await _userRepo.AssignRoleAsync(request.UserId, sellerRole.Id, cancellationToken);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        // 6) Xóa cache danh sách shop (chỉ list, không xóa cache chi tiết)
        _cache.RemoveByPrefix("shops:list:");

        return ApiResponse<Guid>.Succeed(shopId, "Đăng ký bán hàng thành công. Vui lòng chờ Admin duyệt.");
    }
}
