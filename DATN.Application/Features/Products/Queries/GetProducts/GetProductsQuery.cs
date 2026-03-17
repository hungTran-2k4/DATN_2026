using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Domain.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace DATN.Application.Features.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<PagedResponse<IEnumerable<ProductDto>>>
{
    public Guid? ShopId { get; set; }
    public string? Search { get; set; }
    public FilterDescriptor? Filter { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public GetProductsQuery(Guid? shopId = null, string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 20)
    {
        ShopId = shopId;
        Search = search;
        Filter = filter;
        Page = page;
        PageSize = pageSize;
    }
}
