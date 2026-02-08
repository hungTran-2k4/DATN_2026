using AutoMapper;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;

namespace MyProject.Application.Mapping
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
