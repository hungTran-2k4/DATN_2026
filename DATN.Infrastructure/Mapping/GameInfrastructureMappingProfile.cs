using AutoMapper;
using MyProject.Domain.Entities.Stores;
using System;
using System.Runtime.InteropServices;
using DATN.EntityClasses;

namespace MyProject.Infrastructure.Mapping;


public class GameInfrastructureMappingProfile : Profile
{
    public GameInfrastructureMappingProfile()
    {

        // LLBLGen Entity to Domain mappings
        CreateMap<GameEntity, Game>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.FullDescription, opt => opt.MapFrom(src => src.FullDescription))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImage))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.PublisherId, opt => opt.MapFrom(src => src.PublisherId ?? Guid.Empty))
            .ForMember(dest => dest.status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt ?? DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt ?? DateTime.UtcNow))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher));


        // Domain to LLBLGen Entity mappings
        CreateMap<Game, GameEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.FullDescription, opt => opt.MapFrom(src => src.FullDescription))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.CoverImage, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.PublisherId, opt => opt.MapFrom(src => src.PublisherId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            // Ignore all navigation properties and collections
            .ForMember(dest => dest.Carts, opt => opt.Ignore())
            .ForMember(dest => dest.Discounts, opt => opt.Ignore())
            .ForMember(dest => dest.GameTags, opt => opt.Ignore())
            .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.UserGames, opt => opt.Ignore())
            .ForMember(dest => dest.Wishlists, opt => opt.Ignore())
            .ForMember(dest => dest.Publisher, opt => opt.Ignore());
    }
}