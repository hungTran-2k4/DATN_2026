using DATN.Application.Common.Models;
using DATN.Application.DTOs.Marketing;
using DATN.Application.Features.Vouchers.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Vouchers.Handlers;

public class VoucherQueryHandlers :
    IRequestHandler<GetVouchersQuery, PagedResponse<IEnumerable<VoucherDto>>>,
    IRequestHandler<GetActiveVouchersQuery, ApiResponse<IEnumerable<VoucherDto>>>,
    IRequestHandler<GetVoucherByIdQuery, ApiResponse<VoucherDto>>,
    IRequestHandler<GetVoucherByCodeQuery, ApiResponse<VoucherDto>>,
    IRequestHandler<GetUserSavedVouchersQuery, ApiResponse<IEnumerable<VoucherDto>>>
{
    private readonly IVoucherRepository _repository;

    public VoucherQueryHandlers(IVoucherRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<IEnumerable<VoucherDto>>> Handle(GetVouchersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.Search, 
            request.ShopId, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        var dtos = items.Select(v => new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscountValue = v.MaxDiscountValue,
            MinOrderValue = v.MinOrderValue,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount ?? 0,
            IsActive = v.IsActive ?? false,
            ShopId = v.ShopId
        });

        return PagedResponse<IEnumerable<VoucherDto>>.SucceedDefault(dtos.ToList(), request.Page, request.PageSize, total);
    }

    public async Task<ApiResponse<IEnumerable<VoucherDto>>> Handle(GetActiveVouchersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetActiveVouchersAsync(request.ShopId, cancellationToken);
        var dtos = items.Select(v => new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscountValue = v.MaxDiscountValue,
            MinOrderValue = v.MinOrderValue,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount ?? 0,
            IsActive = v.IsActive ?? false,
            ShopId = v.ShopId
        });

        return ApiResponse<IEnumerable<VoucherDto>>.Succeed(dtos);
    }

    public async Task<ApiResponse<VoucherDto>> Handle(GetVoucherByIdQuery request, CancellationToken cancellationToken)
    {
        var voucher = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (voucher == null)
            return ApiResponse<VoucherDto>.Fail("Voucher not found");

        var dto = new VoucherDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            Name = voucher.Name,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountValue = voucher.MaxDiscountValue,
            MinOrderValue = voucher.MinOrderValue,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            UsageLimit = voucher.UsageLimit,
            UsedCount = voucher.UsedCount ?? 0,
            IsActive = voucher.IsActive ?? false,
            ShopId = voucher.ShopId
        };
        return ApiResponse<VoucherDto>.Succeed(dto);
    }

    public async Task<ApiResponse<VoucherDto>> Handle(GetVoucherByCodeQuery request, CancellationToken cancellationToken)
    {
        var voucher = await _repository.GetByCodeAsync(request.Code, request.ShopId, cancellationToken);
        if (voucher == null)
            return ApiResponse<VoucherDto>.Fail("Voucher not found or invalid");

        var dto = new VoucherDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            Name = voucher.Name,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountValue = voucher.MaxDiscountValue,
            MinOrderValue = voucher.MinOrderValue,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            UsageLimit = voucher.UsageLimit,
            UsedCount = voucher.UsedCount ?? 0,
            IsActive = voucher.IsActive ?? false,
            ShopId = voucher.ShopId
        };
        return ApiResponse<VoucherDto>.Succeed(dto);
    }

    public async Task<ApiResponse<IEnumerable<VoucherDto>>> Handle(GetUserSavedVouchersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetUserSavedVouchersAsync(request.UserId, request.IsUsed, cancellationToken);
        var dtos = items.Select(v => new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscountValue = v.MaxDiscountValue,
            MinOrderValue = v.MinOrderValue,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount ?? 0,
            IsActive = v.IsActive ?? false,
            ShopId = v.ShopId
        });

        return ApiResponse<IEnumerable<VoucherDto>>.Succeed(dtos);
    }
}
