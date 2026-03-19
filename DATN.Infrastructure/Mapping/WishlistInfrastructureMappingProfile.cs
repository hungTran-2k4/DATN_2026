using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class WishlistInfrastructureMappingProfile : Profile
{
    public WishlistInfrastructureMappingProfile()
    {
        CreateMap<WishlistEntity, WishlistItem>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

        CreateMap<WishlistItem, WishlistEntity>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore());
    }
}
