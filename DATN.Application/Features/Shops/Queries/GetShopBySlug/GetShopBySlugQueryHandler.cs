using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Shops.Queries.GetShopBySlug;

public class GetShopBySlugQueryHandler : IRequestHandler<GetShopBySlugQuery, ApiResponse<ShopDto>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;

    public GetShopBySlugQueryHandler(IShopRepository shopRepository, IMapper mapper)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShopDto>> Handle(GetShopBySlugQuery request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (shop == null)
            return ApiResponse<ShopDto>.Succeed(null!, "Slug khả dụng.");

        var dto = _mapper.Map<ShopDto>(shop);
        return ApiResponse<ShopDto>.Succeed(dto, "Slug đã được sử dụng.");
    }
}
