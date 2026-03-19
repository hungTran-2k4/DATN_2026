using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN.Domain.Entities.Products;

namespace DATN.Application.Mapping;

public class StockApplicationMappingProfile : Profile
{
    public StockApplicationMappingProfile()
    {
        CreateMap<Stock, StockDto>()
            .ForMember(d => d.VariantId, opt => opt.MapFrom(s => s.Id));
            
        CreateMap<StockTransaction, StockTransactionDto>();
    }
}
