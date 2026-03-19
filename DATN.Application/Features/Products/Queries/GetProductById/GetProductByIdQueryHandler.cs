using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper, ICacheService cache)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var key = $"products:detail:id={request.Id}:shop={request.ShopId}";
        return await _cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var product = await _productRepository.GetByIdAsync(request.Id, request.ShopId, ct);
                if (product == null)
                {
                    return ApiResponse<ProductDto>.Fail("Product not found or does not belong to this shop.", 404);
                }

                var dto = _mapper.Map<ProductDto>(product);
                
                // Fetch images specifically
                var images = await _productRepository.GetImagesAsync(request.Id, ct);
                dto.Images = images.Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    DisplayOrder = i.DisplayOrder ?? 0,
                    IsMain = i.IsMain ?? false
                }).OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).ToList();

                return ApiResponse<ProductDto>.Succeed(dto);
            },
            Ttl,
            cancellationToken);
    }
}
