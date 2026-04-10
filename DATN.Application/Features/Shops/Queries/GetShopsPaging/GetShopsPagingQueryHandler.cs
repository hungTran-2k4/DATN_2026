using AutoMapper;
using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Queries.GetShopsPaging;

public class GetShopsPagingQueryHandler : IRequestHandler<GetShopsPagingQuery, PagedResponse<IEnumerable<ShopDto>>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IMapper _mapper;

    public GetShopsPagingQueryHandler(IShopRepository shopRepository, IMapper mapper)
    {
        _shopRepository = shopRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<ShopDto>>> Handle(GetShopsPagingQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _shopRepository.GetPagedAsync(request.Search, request.Filter, request.Page, request.PageSize, cancellationToken);
        var dtoItems = _mapper.Map<IEnumerable<ShopDto>>(items);
        return PagedResponse<IEnumerable<ShopDto>>.SucceedDefault(dtoItems, request.Page, request.PageSize, totalCount);
    }
}
