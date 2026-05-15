using AutoMapper;
using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.ChangeShopStatus;

public class ChangeShopStatusCommandHandler : IRequestHandler<ChangeShopStatusCommand, ApiResponse<bool>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICacheService _cache;

    public ChangeShopStatusCommandHandler(
        IShopRepository shopRepository, 
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICacheService cache)
    {
        _shopRepository = shopRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(ChangeShopStatusCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);

        if (shop == null)
            return ApiResponse<bool>.Fail("Không tìm thấy Shop.", 404);

        if (request.Status == ShopApprovalStatus.Rejected)
        {
            // Soft reject: chuyển trạng thái sang Rejected và deactivate thay vì xóa vật lý
            shop.ApprovalStatus = ShopApprovalStatus.Rejected;
            shop.IsActive = false;
            await _shopRepository.UpdateAsync(shop, cancellationToken);
            return ApiResponse<bool>.Succeed(true, "Đã từ chối đơn đăng ký Shop.");
        }

        // Nếu Duyệt (Approve) -> Gán role Seller cho chủ shop
        if (request.Status == ShopApprovalStatus.Approved && shop.OwnerId.HasValue)
        {
            var sellerRole = await _roleRepository.GetByNameAsync("Seller", cancellationToken);
            if (sellerRole == null)
            {
                // Fallback nếu tên role trong DB khác hoa thường
                sellerRole = await _roleRepository.GetByNameAsync("SELLER", cancellationToken);
            }

            if (sellerRole != null)
            {
                var userRoles = await _userRepository.GetUserRolesAsync(shop.OwnerId.Value, cancellationToken);
                bool hasSellerRole = userRoles.Any(r => r.Equals("Seller", StringComparison.OrdinalIgnoreCase));

                if (!hasSellerRole)
                {
                    await _userRepository.AssignRoleAsync(shop.OwnerId.Value, sellerRole.Id, cancellationToken);
                }
            }
        }

        shop.ApprovalStatus = request.Status;
        await _shopRepository.UpdateAsync(shop, cancellationToken);

        _cache.RemoveByPrefix("shops:list:");

        string message = request.Status switch
        {
            ShopApprovalStatus.Approved => "Shop đã được duyệt thành công.",
            ShopApprovalStatus.Suspended => "Shop đã bị khoá.",
            ShopApprovalStatus.Pending => "Shop đã được chuyển về trạng thái chờ duyệt.",
            _ => "Cập nhật trạng thái thành công."
        };

        return ApiResponse<bool>.Succeed(true, message);
    }
}
