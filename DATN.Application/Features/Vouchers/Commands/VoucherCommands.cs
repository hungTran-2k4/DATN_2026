using DATN.Application.Common.Models;
using DATN.Application.DTOs.Marketing;
using MediatR;

namespace DATN.Application.Features.Vouchers.Commands;

public class CreateVoucherCommand : IRequest<ApiResponse<VoucherDto>>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ShopId { get; set; }
}

public class UpdateVoucherCommand : IRequest<ApiResponse<VoucherDto>>
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public bool IsActive { get; set; }
}

public record DeleteVoucherCommand(Guid Id) : IRequest<ApiResponse<bool>>;

public record SaveVoucherCommand(Guid VoucherId) : IRequest<ApiResponse<bool>>;

public record ApplyVoucherCommand(Guid VoucherId) : IRequest<ApiResponse<bool>>;
