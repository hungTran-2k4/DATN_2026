using AutoMapper;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Entities.Categories;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

/// <summary>
/// AutoMapper profiles cho UserAddress và Category mappings
/// </summary>
public class MvpInfrastructureMappingProfile : Profile
{
    public MvpInfrastructureMappingProfile()
    {
        // ── UserAddress ──
        CreateMap<UserAddressEntity, UserAddress>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.ProvinceId, o => o.MapFrom(s => s.ProvinceId))
            .ForMember(d => d.DistrictId, o => o.MapFrom(s => s.DistrictId))
            .ForMember(d => d.WardId, o => o.MapFrom(s => s.WardId))
            .ForMember(d => d.DetailedAddress, o => o.MapFrom(s => s.DetailedAddress))
            .ForMember(d => d.IsDefault, o => o.MapFrom(s => s.IsDefault))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

        CreateMap<UserAddress, UserAddressEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.PhoneNumber))
            .ForMember(d => d.ProvinceId, o => o.MapFrom(s => s.ProvinceId))
            .ForMember(d => d.DistrictId, o => o.MapFrom(s => s.DistrictId))
            .ForMember(d => d.WardId, o => o.MapFrom(s => s.WardId))
            .ForMember(d => d.DetailedAddress, o => o.MapFrom(s => s.DetailedAddress))
            .ForMember(d => d.IsDefault, o => o.MapFrom(s => s.IsDefault))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.User, o => o.Ignore());

        // ── Category ──
        CreateMap<CategoryEntity, Category>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Slug, o => o.MapFrom(s => s.Slug))
            .ForMember(d => d.IconUrl, o => o.MapFrom(s => s.IconUrl))
            .ForMember(d => d.ParentId, o => o.MapFrom(s => s.ParentId))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.DisplayOrder, o => o.MapFrom(s => s.DisplayOrder))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.Children, o => o.Ignore());

        CreateMap<Category, CategoryEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Slug, o => o.MapFrom(s => s.Slug))
            .ForMember(d => d.IconUrl, o => o.MapFrom(s => s.IconUrl))
            .ForMember(d => d.ParentId, o => o.MapFrom(s => s.ParentId))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.DisplayOrder, o => o.MapFrom(s => s.DisplayOrder))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.Categories, o => o.Ignore())
            .ForMember(d => d.Products, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore());
    }
}
