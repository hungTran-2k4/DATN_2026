using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using MediatR;

namespace DATN.Application.Features.Shops.Queries.GetShopBySlug;

public class GetShopBySlugQuery : IRequest<ApiResponse<ShopDto>>
{
    public string Slug { get; set; }

    public GetShopBySlugQuery(string slug)
    {
        Slug = slug;
    }
}
