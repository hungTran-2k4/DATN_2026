using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class ReviewInfrastructureMappingProfile : Profile
{
    public ReviewInfrastructureMappingProfile()
    {
        CreateMap<ReviewEntity, Review>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.VariantId, o => o.MapFrom(s => s.VariantId))
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
            .ForMember(d => d.Rating, o => o.MapFrom(s => s.Rating))
            .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

        CreateMap<Review, ReviewEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.VariantId, o => o.MapFrom(s => s.VariantId))
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
            .ForMember(d => d.Rating, o => o.MapFrom(s => s.Rating))
            .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.ProductVariant, o => o.Ignore())
            .ForMember(d => d.Order, o => o.Ignore());
    }
}
