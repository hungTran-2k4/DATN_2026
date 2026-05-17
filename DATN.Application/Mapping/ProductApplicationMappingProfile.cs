using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN.Domain.Entities.Products;
using DATN.Domain.Enums;
using System.Collections.Generic;

namespace DATN.Application.Mapping;

public class ProductApplicationMappingProfile : Profile
{
    public ProductApplicationMappingProfile()
    {
        // Product.Status (enum) → ProductDto.Status (string)
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToStatusString()))
            .ReverseMap()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToProductStatus()));

        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(d => d.StockQty, o => o.MapFrom(s => s.StockQty))
            .ForMember(d => d.VariantAttributes, o => o.MapFrom(s =>
                s.VariantAttributes != null
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(s.VariantAttributes, (System.Text.Json.JsonSerializerOptions?)null)
                    : null));
    }
}
