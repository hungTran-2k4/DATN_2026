using AutoMapper;
using DATN.Domain.Entities.Marketing;
using DATN_2026.EntityClasses;

namespace DATN.Infrastructure.Mapping;

public class VoucherInfrastructureMappingProfile : Profile
{
    public VoucherInfrastructureMappingProfile()
    {
        CreateMap<VoucherEntity, Voucher>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Code, o => o.MapFrom(s => s.Code))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.DiscountType, o => o.MapFrom(s => s.DiscountType))
            .ForMember(d => d.DiscountValue, o => o.MapFrom(s => s.DiscountValue))
            .ForMember(d => d.MaxDiscountValue, o => o.MapFrom(s => s.MaxDiscountValue))
            .ForMember(d => d.MinOrderValue, o => o.MapFrom(s => s.MinOrderValue))
            .ForMember(d => d.StartDate, o => o.MapFrom(s => s.StartDate))
            .ForMember(d => d.EndDate, o => o.MapFrom(s => s.EndDate))
            .ForMember(d => d.UsageLimit, o => o.MapFrom(s => s.UsageLimit))
            .ForMember(d => d.UsedCount, o => o.MapFrom(s => s.UsedCount))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId));

        CreateMap<Voucher, VoucherEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Code, o => o.MapFrom(s => s.Code))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.DiscountType, o => o.MapFrom(s => s.DiscountType))
            .ForMember(d => d.DiscountValue, o => o.MapFrom(s => s.DiscountValue))
            .ForMember(d => d.MaxDiscountValue, o => o.MapFrom(s => s.MaxDiscountValue))
            .ForMember(d => d.MinOrderValue, o => o.MapFrom(s => s.MinOrderValue))
            .ForMember(d => d.StartDate, o => o.MapFrom(s => s.StartDate))
            .ForMember(d => d.EndDate, o => o.MapFrom(s => s.EndDate))
            .ForMember(d => d.UsageLimit, o => o.MapFrom(s => s.UsageLimit))
            .ForMember(d => d.UsedCount, o => o.MapFrom(s => s.UsedCount))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
            .ForMember(d => d.Shop, o => o.Ignore())
            .ForMember(d => d.UserVouchers, o => o.Ignore());

        CreateMap<UserVoucherEntity, UserVoucher>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.VoucherId, o => o.MapFrom(s => s.VoucherId))
            .ForMember(d => d.IsUsed, o => o.MapFrom(s => s.IsUsed))
            .ForMember(d => d.SavedAt, o => o.MapFrom(s => s.SavedAt));

        CreateMap<UserVoucher, UserVoucherEntity>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.VoucherId, o => o.MapFrom(s => s.VoucherId))
            .ForMember(d => d.IsUsed, o => o.MapFrom(s => s.IsUsed))
            .ForMember(d => d.SavedAt, o => o.MapFrom(s => s.SavedAt))
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Voucher, o => o.Ignore());
    }
}
