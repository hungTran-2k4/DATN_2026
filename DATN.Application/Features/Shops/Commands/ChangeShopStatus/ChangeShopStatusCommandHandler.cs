using AutoMapper;
using MediatR;
using DATN.Application.Common.Models;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.ChangeShopStatus;

public class ChangeShopStatusCommandHandler : IRequestHandler<ChangeShopStatusCommand, ApiResponse<bool>>
{
    private readonly IShopRepository _shopRepository;

    public ChangeShopStatusCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ApiResponse<bool>> Handle(ChangeShopStatusCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        
        if (shop == null)
            return ApiResponse<bool>.Fail("Không tìm thấy Shop.", 404);

        if (request.Status == ShopApprovalStatus.Rejected)
            {
            // For now, if a shop is rejected, we simply logically or physically remove it to save space
            // Or we could map it to IsActive = false, but what if they want to re-register?
            // Deleting is a better approach until we have an explicit Rejected DB column
            await _shopRepository.DeleteAsync(shop.Id, cancellationToken);
            return ApiResponse<bool>.Succeed(true, "Shop đã bị từ chối và khoá vĩnh viễn (xóa khỏi hệ thống).");
        }

        shop.ApprovalStatus = request.Status;
        await _shopRepository.UpdateAsync(shop, cancellationToken);

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
