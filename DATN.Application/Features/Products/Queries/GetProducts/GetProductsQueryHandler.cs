using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<IEnumerable<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _productRepository.GetPagedAsync(request.ShopId, request.Search, request.Page, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);
        return PagedResponse<IEnumerable<ProductDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}
