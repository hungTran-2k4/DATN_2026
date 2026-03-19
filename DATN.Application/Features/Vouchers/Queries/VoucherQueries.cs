using DATN.Application.Common.Models;
using DATN.Application.DTOs.Marketing;
using MediatR;

namespace DATN.Application.Features.Vouchers.Queries;

public record GetVouchersQuery(
    string? Search = null,
    Guid? ShopId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<IEnumerable<VoucherDto>>>;

public record GetActiveVouchersQuery(Guid? ShopId = null) : IRequest<ApiResponse<IEnumerable<VoucherDto>>>;

public record GetVoucherByIdQuery(Guid Id) : IRequest<ApiResponse<VoucherDto>>;

public record GetVoucherByCodeQuery(string Code, Guid? ShopId = null) : IRequest<ApiResponse<VoucherDto>>;

public record GetUserSavedVouchersQuery(Guid UserId, bool IsUsed = false) : IRequest<ApiResponse<IEnumerable<VoucherDto>>>;
