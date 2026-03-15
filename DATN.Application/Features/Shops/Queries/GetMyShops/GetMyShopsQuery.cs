using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using MediatR;
using System;
using System.Collections.Generic;

namespace DATN.Application.Features.Shops.Queries.GetMyShops;

public class GetMyShopsQuery : IRequest<ApiResponse<IEnumerable<ShopDto>>>
{
    public Guid OwnerId { get; set; }

    public GetMyShopsQuery(Guid ownerId)
    {
        OwnerId = ownerId;
    }
}
