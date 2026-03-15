using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Queries.GetMyShops;

public class GetMyShopsQueryHandler : IRequestHandler<GetMyShopsQuery, ApiResponse<IEnumerable<ShopDto>>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;

    public GetMyShopsQueryHandler(IShopRepository shopRepository, IMapper mapper)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<ShopDto>>> Handle(GetMyShopsQuery request, CancellationToken cancellationToken)
    {
        var shops = await _shopRepository.GetByOwnerIdAsync(request.OwnerId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<ShopDto>>(shops);
        return ApiResponse<IEnumerable<ShopDto>>.Succeed(dtos);
    }
}
