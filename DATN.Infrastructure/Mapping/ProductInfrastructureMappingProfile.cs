using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN_2026.EntityClasses;

using DATN.Domain.Entities.Products;

namespace DATN.Infrastructure.Mapping;

public class ProductInfrastructureMappingProfile : Profile
{
    public ProductInfrastructureMappingProfile()
    {
        CreateMap<ProductEntity, Product>().ReverseMap();
    }
}
