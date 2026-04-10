using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Common.Models;
using System.Collections.Generic;

namespace DATN.Application.Features.Shops.Queries.GetShopsPaging;

public class GetShopsPagingQuery : PagedRequest, IRequest<PagedResponse<IEnumerable<ShopDto>>>
{
    public GetShopsPagingQuery(string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 10)
        : base(search, filter, page, pageSize)
    {
    }
}
