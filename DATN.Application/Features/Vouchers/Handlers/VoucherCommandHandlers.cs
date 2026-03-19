using DATN.Application.Common.Models;
using DATN.Application.DTOs.Marketing;
using DATN.Application.Features.Vouchers.Commands;
using DATN.Domain.Entities.Marketing;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Vouchers.Handlers;

public class VoucherCommandHandlers :
    IRequestHandler<CreateVoucherCommand, ApiResponse<VoucherDto>>,
    IRequestHandler<UpdateVoucherCommand, ApiResponse<VoucherDto>>,
    IRequestHandler<DeleteVoucherCommand, ApiResponse<bool>>,
    IRequestHandler<SaveVoucherCommand, ApiResponse<bool>>,
    IRequestHandler<ApplyVoucherCommand, ApiResponse<bool>>
{
    private readonly IVoucherRepository _repository;
    
    // In a real scenario we'd inject ICurrentUserService to get the user ID for Save/Apply
    // For now we assume a fallback or placeholder logic as implemented elsewhere

    public VoucherCommandHandlers(IVoucherRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<VoucherDto>> Handle(CreateVoucherCommand request, CancellationToken cancellationToken)
    {
        // Check if code already exists
        var existing = await _repository.GetByCodeAsync(request.Code, request.ShopId, cancellationToken);
        if (existing != null)
            return ApiResponse<VoucherDto>.Fail("Voucher code already exists for this shop.");

        var entity = new Voucher
        {
            Code = request.Code,
            Name = request.Name,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxDiscountValue = request.MaxDiscountValue,
            MinOrderValue = request.MinOrderValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            IsActive = request.IsActive,
            ShopId = request.ShopId
        };

        var created = await _repository.AddAsync(entity, cancellationToken);
        
        var dto = new VoucherDto
        {
            Id = created.Id,
            Code = created.Code,
            Name = created.Name,
            DiscountType = created.DiscountType,
            DiscountValue = created.DiscountValue,
            MaxDiscountValue = created.MaxDiscountValue,
            MinOrderValue = created.MinOrderValue,
            StartDate = created.StartDate,
            EndDate = created.EndDate,
            UsageLimit = created.UsageLimit,
            UsedCount = created.UsedCount ?? 0,
            IsActive = created.IsActive ?? false,
            ShopId = created.ShopId
        };

        return ApiResponse<VoucherDto>.Succeed(dto, "Voucher created successfully");
    }

    public async Task<ApiResponse<VoucherDto>> Handle(UpdateVoucherCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return ApiResponse<VoucherDto>.Fail("Voucher not found");

        if (!existing.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase))
        {
            var codeConflict = await _repository.GetByCodeAsync(request.Code, existing.ShopId, cancellationToken);
            if (codeConflict != null)
                return ApiResponse<VoucherDto>.Fail("New voucher code already exists for this shop.");
        }

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.DiscountType = request.DiscountType;
        existing.DiscountValue = request.DiscountValue;
        existing.MaxDiscountValue = request.MaxDiscountValue;
        existing.MinOrderValue = request.MinOrderValue;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.UsageLimit = request.UsageLimit;
        existing.IsActive = request.IsActive;

        var result = await _repository.UpdateAsync(existing, cancellationToken);
        if (!result) return ApiResponse<VoucherDto>.Fail("Failed to update voucher");

        var dto = new VoucherDto
        {
            Id = existing.Id,
            Code = existing.Code,
            Name = existing.Name,
            DiscountType = existing.DiscountType,
            DiscountValue = existing.DiscountValue,
            MaxDiscountValue = existing.MaxDiscountValue,
            MinOrderValue = existing.MinOrderValue,
            StartDate = existing.StartDate,
            EndDate = existing.EndDate,
            UsageLimit = existing.UsageLimit,
            UsedCount = existing.UsedCount ?? 0,
            IsActive = existing.IsActive ?? false,
            ShopId = existing.ShopId
        };

        return ApiResponse<VoucherDto>.Succeed(dto, "Voucher updated successfully");
    }

    public async Task<ApiResponse<bool>> Handle(DeleteVoucherCommand request, CancellationToken cancellationToken)
    {
        var result = await _repository.DeleteAsync(request.Id, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Voucher deleted successfully")
            : ApiResponse<bool>.Fail("Voucher not found or could not be deleted");
    }

    public async Task<ApiResponse<bool>> Handle(SaveVoucherCommand request, CancellationToken cancellationToken)
    {
        // Placeholder UserId - replace with actual ICurrentUserService resolution if available in environment
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var hasSaved = await _repository.HasUserSavedVoucherAsync(userId, request.VoucherId, cancellationToken);
        if (hasSaved)
            return ApiResponse<bool>.Fail("Voucher already saved by user");

        var userVoucher = new UserVoucher
        {
            UserId = userId,
            VoucherId = request.VoucherId
        };
        
        var result = await _repository.SaveVoucherForUserAsync(userVoucher, cancellationToken);
        return result
            ? ApiResponse<bool>.Succeed(true, "Voucher saved successfully")
            : ApiResponse<bool>.Fail("Failed to save voucher");
    }

    public async Task<ApiResponse<bool>> Handle(ApplyVoucherCommand request, CancellationToken cancellationToken)
    {
        // Placeholder UserId 
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Logic check: Validate voucher is active, date is valid, usage limits, min order value (would typically be checked against order here)
        var result = await _repository.MarkVoucherAsUsedAsync(userId, request.VoucherId, cancellationToken);
        return result
            ? ApiResponse<bool>.Succeed(true, "Voucher applied/used successfully")
            : ApiResponse<bool>.Fail("Voucher could not be applied or was not saved by user");
    }
}
