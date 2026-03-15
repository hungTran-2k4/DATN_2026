using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using MediatR;
using System.Collections.Generic;

namespace DATN.Application.Features.Shops.Queries.GetShops;

public class GetShopsQuery : IRequest<ApiResponse<IEnumerable<ShopDto>>>
{
}
