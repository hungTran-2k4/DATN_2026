using AutoMapper;
using DATN.EntityClasses;
using MyProject.Domain.Entities.Identity;

namespace MyProject.Infrastructure.Mapping
{
    public class RefreshTokenInfrastructureMappingProfile : Profile
    {
        public RefreshTokenInfrastructureMappingProfile()
        {
            CreateMap<RefreshTokenEntity, RefreshToken>()
                .ForMember(dest => dest.ReplaceByTokenId, opt => opt.MapFrom(src => src.ReplacedByTokenId))
                .ReverseMap()
                .ForMember(dest => dest.ReplacedByTokenId, opt => opt.MapFrom(src => src.ReplaceByTokenId));
        }
    }
}
