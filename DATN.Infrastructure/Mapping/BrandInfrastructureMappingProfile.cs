using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class BrandInfrastructureMappingProfile : Profile
{
    public BrandInfrastructureMappingProfile()
    {
        CreateMap<BrandEntity, Brand>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Slug, o => o.MapFrom(s => s.Slug))
            .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoUrl))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        CreateMap<Brand, BrandEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Slug, o => o.MapFrom(s => s.Slug))
            .ForMember(d => d.LogoUrl, o => o.MapFrom(s => s.LogoUrl))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.Products, o => o.Ignore());
    }
}
