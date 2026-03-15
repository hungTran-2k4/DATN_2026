using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN.Domain.Entities.Products;

namespace DATN.Application.Mapping;

public class ProductApplicationMappingProfile : Profile
{
    public ProductApplicationMappingProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
    }
}
