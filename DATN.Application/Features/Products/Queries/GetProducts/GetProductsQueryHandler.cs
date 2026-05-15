using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using DATN.Domain.Enums;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace DATN.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<IEnumerable<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public GetProductsQueryHandler(IProductRepository productRepository, IMapper mapper, ICacheService cache)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResponse<IEnumerable<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Xác định trạng thái cần lọc:
        // 1. Nếu IncludeInactive = true -> Không lọc theo trạng thái (Lấy tất cả - Dành cho Seller/Admin)
        // 2. Nếu có Status cụ thể -> Lọc theo status đó
        // 3. Nếu không có gì -> Mặc định là Active (Dành cho khách hàng)
        ProductStatus? statusToFilter = null;
        if (request.IncludeInactive != true)
        {
            statusToFilter = request.Status ?? ProductStatus.Active;
        }

        // Cache paged lists briefly to reduce DB load (especially for homepage/search).
        // Key includes shopId/search/page/pageSize/status/includeInactive and a stable filter representation.
        var filterKey = request.Filter == null ? "null" : JsonSerializer.Serialize(request.Filter);
        var key = $"products:paging:shop={request.ShopId}:status={statusToFilter}:inc={request.IncludeInactive}:search={request.Search}:page={request.Page}:size={request.PageSize}:filter={filterKey}";

        return await _cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var (items, total) = await _productRepository.GetPagedAsync(
                    request.ShopId, request.Search, request.Filter, statusToFilter, request.Page, request.PageSize, ct);
                var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);
                return PagedResponse<IEnumerable<ProductDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
            },
            Ttl,
            cancellationToken);
    }
}
