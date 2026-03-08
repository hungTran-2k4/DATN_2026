using AutoMapper;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;

namespace DATN.Application.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles are handled separately
        }
    }
}
