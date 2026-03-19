using AutoMapper;
using DATN.Application.DTOs.Products;
using DATN.Domain.Entities.Products;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class StockInfrastructureMappingProfile : Profile
{
    public StockInfrastructureMappingProfile()
    {
        CreateMap<StockEntity, Stock>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.VariantId)); // Map PK

        CreateMap<Stock, StockEntity>()
            .ForMember(d => d.VariantId, opt => opt.MapFrom(s => s.Id));

        CreateMap<StockTransactionEntity, StockTransaction>();
        CreateMap<StockTransaction, StockTransactionEntity>();
    }
}
