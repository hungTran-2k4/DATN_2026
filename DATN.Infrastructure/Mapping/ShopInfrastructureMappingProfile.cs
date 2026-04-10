using AutoMapper;
using DATN.Domain.Entities.Shops;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class ShopInfrastructureMappingProfile : Profile
{
    public ShopInfrastructureMappingProfile()
    {
        CreateMap<ShopEntity, Shop>()
            .ForMember(d => d.ApprovalStatus, opt => opt.MapFrom(s => s.IsActive == null ? DATN.Domain.Enums.ShopApprovalStatus.Pending : (s.IsActive == true ? DATN.Domain.Enums.ShopApprovalStatus.Approved : DATN.Domain.Enums.ShopApprovalStatus.Suspended)))
            .ForMember(d => d.OwnerName, opt => opt.MapFrom(s => s.User != null ? s.User.Username : null))
            .ForMember(d => d.OwnerEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email : null));
        CreateMap<Shop, ShopEntity>()
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.ApprovalStatus == DATN.Domain.Enums.ShopApprovalStatus.Pending ? (bool?)null : (s.ApprovalStatus == DATN.Domain.Enums.ShopApprovalStatus.Approved ? true : false)));
    }
}
