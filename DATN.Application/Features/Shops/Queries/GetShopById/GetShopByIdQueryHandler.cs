using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Queries.GetShopById;

public class GetShopByIdQueryHandler : IRequestHandler<GetShopByIdQuery, ApiResponse<ShopDto>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;

    public GetShopByIdQueryHandler(IShopRepository shopRepository, IMapper mapper)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShopDto>> Handle(GetShopByIdQuery request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shop == null)
        {
            return ApiResponse<ShopDto>.Fail("Shop not found.", 404);
        }

        var dto = _mapper.Map<ShopDto>(shop);
        return ApiResponse<ShopDto>.Succeed(dto);
    }
}
