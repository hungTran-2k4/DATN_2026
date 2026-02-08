using AutoMapper;
using DATN.EntityClasses;
using MyProject.Domain.Entities.Identity;

namespace MyProject.Infrastructure.Mapping
{
    public class RefreshTokenInfrastructureMappingProfile : Profile
    {
        public RefreshTokenInfrastructureMappingProfile()
        {
            CreateMap<RefreshTokenEntity, RefreshToken>().ReverseMap();
        }
    }
}
