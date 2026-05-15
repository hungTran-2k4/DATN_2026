using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Domain.Common.Models;
using DATN.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;

namespace DATN.Application.Features.Products.Queries.GetProducts;

public class GetProductsQuery : PagedRequest, IRequest<PagedResponse<IEnumerable<ProductDto>>>
{
    public Guid? ShopId { get; set; }
    public ProductStatus? Status { get; set; }
    public bool? IncludeInactive { get; set; }

    public GetProductsQuery(Guid? shopId = null, string? search = null, FilterDescriptor? filter = null, ProductStatus? status = null, bool? includeInactive = null, int page = 1, int pageSize = 20)
        : base(search, filter, page, pageSize)
    {
        ShopId = shopId;
        Status = status;
        IncludeInactive = includeInactive;
    }
}
