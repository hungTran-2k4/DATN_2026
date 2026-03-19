using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Queries.GetShops;

public class GetShopsQueryHandler : IRequestHandler<GetShopsQuery, ApiResponse<IEnumerable<ShopDto>>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public GetShopsQueryHandler(IShopRepository shopRepository, IMapper mapper, ICacheService cache)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ApiResponse<IEnumerable<ShopDto>>> Handle(GetShopsQuery request, CancellationToken cancellationToken)
    {
        const string key = "shops:all";
        return await _cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var shops = await _shopRepository.GetAllAsync(ct);
                var dtos = _mapper.Map<IEnumerable<ShopDto>>(shops);
                return ApiResponse<IEnumerable<ShopDto>>.Succeed(dtos);
            },
            Ttl,
            cancellationToken);
    }
}
