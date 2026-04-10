using MediatR;
using DATN.Application.Common.Models;
using DATN.Domain.Enums;
using System;

namespace DATN.Application.Features.Shops.Commands.ChangeShopStatus;

public class ChangeShopStatusCommand : IRequest<ApiResponse<bool>>
{
    public Guid ShopId { get; set; }
    public ShopApprovalStatus Status { get; set; }

    public ChangeShopStatusCommand(Guid shopId, ShopApprovalStatus status)
    {
        ShopId = shopId;
        Status = status;
    }
}
