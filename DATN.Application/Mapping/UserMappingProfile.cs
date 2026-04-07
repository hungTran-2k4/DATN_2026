using AutoMapper;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Extensions;

namespace DATN.Application.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.AccountStatus.ToDatabaseString()));
        }
    }
}
