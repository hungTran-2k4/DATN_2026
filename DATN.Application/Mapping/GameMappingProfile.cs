using AutoMapper;
using MyProject.Domain.Entities.Stores;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Mapping;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        // Domain to DTO mappings
        CreateMap<Game, GameBaseRespone>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.FullDescription, opt => opt.MapFrom(src => src.FullDescription ?? string.Empty))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.CoverImage, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate.HasValue ? src.ReleaseDate.Value.ToString("yyyy-MM-dd") : null))
            .ForMember(dest => dest.PublisherId, opt => opt.MapFrom(src => src.PublisherId))
            .ForMember(dest => dest.PublisherName, opt => opt.MapFrom(src => src.Publisher != null ? src.Publisher.Name : null)); // Navigation property - ignore for now


        CreateMap<Game, GetGameByIdRespone>()
            .IncludeBase<Game, GameBaseRespone>();
        // Note: LLBLGen Entity mappings will be handled in Infrastructure layer
        // to avoid circular dependencies
    }
}