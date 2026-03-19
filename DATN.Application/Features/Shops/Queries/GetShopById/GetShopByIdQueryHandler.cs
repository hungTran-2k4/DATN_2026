using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Queries.GetShopById;

public class GetShopByIdQueryHandler : IRequestHandler<GetShopByIdQuery, ApiResponse<ShopDto>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public GetShopByIdQueryHandler(IShopRepository shopRepository, IMapper mapper, ICacheService cache)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ApiResponse<ShopDto>> Handle(GetShopByIdQuery request, CancellationToken cancellationToken)
    {
        var key = $"shops:detail:{request.Id}";
        return await _cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var shop = await _shopRepository.GetByIdAsync(request.Id, ct);
                if (shop == null)
                {
                    return ApiResponse<ShopDto>.Fail("Shop not found.", 404);
                }

                var dto = _mapper.Map<ShopDto>(shop);
                return ApiResponse<ShopDto>.Succeed(dto);
            },
            Ttl,
            cancellationToken);
    }
}
