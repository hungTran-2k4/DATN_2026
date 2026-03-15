using AutoMapper;
using DATN.Application.DTOs.Shops;
using DATN.Domain.Entities.Shops;

namespace DATN.Application.Mapping;

public class ShopApplicationMappingProfile : Profile
{
    public ShopApplicationMappingProfile()
    {
        CreateMap<Shop, ShopDto>().ReverseMap();
    }
}
