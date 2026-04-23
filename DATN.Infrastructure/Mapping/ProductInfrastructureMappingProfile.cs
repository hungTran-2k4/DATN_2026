using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN_2026.EntityClasses;
using DATN.Domain.Entities.Products;
using DATN.Domain.Enums;

namespace DATN.Infrastructure.Mapping;

public class ProductInfrastructureMappingProfile : Profile
{
    public ProductInfrastructureMappingProfile()
    {
        CreateMap<ProductVariantEntity, ProductVariant>()
            .ForMember(d => d.StockQty, o => o.MapFrom(s => s.Stock != null ? s.Stock.AvailableQuantity : 0))
            .ReverseMap();

        // ProductEntity.Status (string) ↔ Product.Status (ProductStatus enum)
        CreateMap<ProductEntity, Product>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToProductStatus()))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.ProductImages))
            .ForMember(d => d.Variants, o => o.MapFrom(s => s.ProductVariants))
            .ReverseMap()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToStatusString()))
            .ForMember(d => d.ProductImages, o => o.Ignore())
            .ForMember(d => d.ProductVariants, o => o.Ignore());
    }
}
