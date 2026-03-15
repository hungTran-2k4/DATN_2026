using DATN.Application.Common.Models;
using MediatR;
using System;

namespace DATN.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid? ShopId { get; set; }

    public DeleteProductCommand(Guid id, Guid? shopId = null)
    {
        Id = id;
        ShopId = shopId;
    }
}
