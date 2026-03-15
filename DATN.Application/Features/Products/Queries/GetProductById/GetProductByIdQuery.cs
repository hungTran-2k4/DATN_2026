using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;
using System;

namespace DATN.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQuery : IRequest<ApiResponse<ProductDto>>
{
    public Guid Id { get; set; }
    public Guid? ShopId { get; set; }

    public GetProductByIdQuery(Guid id, Guid? shopId = null)
    {
        Id = id;
        ShopId = shopId;
    }
}
