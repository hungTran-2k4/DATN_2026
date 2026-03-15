using DATN.Application.Common.Models;
using MediatR;
using System;

namespace DATN.Application.Features.Shops.Commands.DeleteShop;

public class DeleteShopCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; } // Required to verify ownership before deletion

    public DeleteShopCommand(Guid id, Guid? ownerId = null)
    {
        Id = id;
        OwnerId = ownerId;
    }
}
