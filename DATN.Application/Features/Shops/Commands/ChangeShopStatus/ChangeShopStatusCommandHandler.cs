using AutoMapper;
using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.ChangeShopStatus;

public class ChangeShopStatusCommandHandler : IRequestHandler<ChangeShopStatusCommand, ApiResponse<bool>>
{
    private readonly IShopRepository _shopRepository;
    private readonly ICacheService _cache;

    public ChangeShopStatusCommandHandler(IShopRepository shopRepository, ICacheService cache)
    {
        _shopRepository = shopRepository;
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
            // Giúp lưu lịch sử và ngăn spam đăng ký lại với cùng thông tin
            shop.ApprovalStatus = ShopApprovalStatus.Rejected;
            shop.IsActive = false;
            await _shopRepository.UpdateAsync(shop, cancellationToken);
            return ApiResponse<bool>.Succeed(true, "Đã từ chối đơn đăng ký Shop.");
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
