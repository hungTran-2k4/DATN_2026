using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, request.ShopId, cancellationToken);
        if (product == null)
        {
            return ApiResponse<ProductDto>.Fail("Product not found or does not belong to this shop.", 404);
        }

        var dto = _mapper.Map<ProductDto>(product);
        return ApiResponse<ProductDto>.Succeed(dto);
    }
}
