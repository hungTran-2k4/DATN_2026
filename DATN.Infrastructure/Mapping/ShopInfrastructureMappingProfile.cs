using AutoMapper;
using DATN.Domain.Entities.Shops;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class ShopInfrastructureMappingProfile : Profile
{
    public ShopInfrastructureMappingProfile()
    {
        CreateMap<ShopEntity, Shop>().ReverseMap();
    }
}
