using AutoMapper;
using DATN_2026.EntityClasses;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Extensions;

namespace DATN.Infrastructure.Mapping;

/// <summary>
/// AutoMapper profile cho User và Role mappings
/// </summary>
public class UserInfrastructureMappingProfile : Profile
{
    public UserInfrastructureMappingProfile()
    {
        // UserEntity -> User (Domain)
        CreateMap<UserEntity, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.PasswordHash))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Username)) // Username làm FullName (backward compat)
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.AccountStatus, opt => opt.MapFrom(src => UserAccountStatusExtensions.FromDatabaseString(src.Status)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.FailedLoginCount, opt => opt.MapFrom(src => src.FailedLoginCount))
            .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
            .ForMember(dest => dest.UserRoles, opt => opt.MapFrom(src => src.UserRoles)); // Map navigation properties

        // UserRoleEntity -> UserRole (Domain)
        CreateMap<UserRoleEntity, UserRole>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        // User (Domain) -> UserEntity
        CreateMap<User, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Username) ? src.Username : src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.PasswordHash))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.AccountStatus.ToDatabaseString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.FailedLoginCount, opt => opt.MapFrom(src => src.FailedLoginCount))
            .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd))
            // Ignore navigation properties
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.UserSessions, opt => opt.Ignore())
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
            .ForMember(dest => dest.Shops, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.Carts, opt => opt.Ignore())
            .ForMember(dest => dest.Orders, opt => opt.Ignore());

        // RoleEntity -> Role (Domain)
        CreateMap<RoleEntity, Role>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // Role (Domain) -> RoleEntity
        CreateMap<Role, RoleEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
            // Ignore navigation properties
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());
    }
}
