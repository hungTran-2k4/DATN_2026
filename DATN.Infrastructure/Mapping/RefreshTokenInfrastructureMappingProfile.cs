using AutoMapper;
using DATN.EntityClasses;
using DATN.Domain.Entities.Identity;

namespace DATN.Infrastructure.Mapping
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
