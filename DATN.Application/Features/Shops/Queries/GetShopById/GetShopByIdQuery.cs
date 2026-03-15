using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using MediatR;
using System;

namespace DATN.Application.Features.Shops.Queries.GetShopById;

public class GetShopByIdQuery : IRequest<ApiResponse<ShopDto>>
{
    public Guid Id { get; set; }

    public GetShopByIdQuery(Guid id)
    {
        Id = id;
    }
}
