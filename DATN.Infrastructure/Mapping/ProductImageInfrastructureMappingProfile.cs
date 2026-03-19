using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN_2026.EntityClasses;
using DATN.Application.DTOs.Products;

namespace DATN.Infrastructure.Mapping;

public class ProductImageInfrastructureMappingProfile : Profile
{
    public ProductImageInfrastructureMappingProfile()
    {
        CreateMap<ProductImageEntity, ProductImage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
            .ForMember(d => d.ImageUrl, o => o.MapFrom(s => s.Url))
            .ForMember(d => d.DisplayOrder, o => o.MapFrom(s => s.DisplayOrder))
            .ForMember(d => d.IsMain, o => o.MapFrom(s => s.IsPrimary))
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        CreateMap<ProductImage, ProductImageEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
            .ForMember(d => d.Url, o => o.MapFrom(s => s.ImageUrl))
            .ForMember(d => d.DisplayOrder, o => o.MapFrom(s => s.DisplayOrder))
            .ForMember(d => d.IsPrimary, o => o.MapFrom(s => s.IsMain))
            .ForMember(d => d.Product, o => o.Ignore());
            
        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
            .ForMember(d => d.ImageUrl, o => o.MapFrom(s => s.ImageUrl))
            .ForMember(d => d.DisplayOrder, o => o.MapFrom(s => s.DisplayOrder ?? 0))
            .ForMember(d => d.IsMain, o => o.MapFrom(s => s.IsMain ?? false));
    }
}
