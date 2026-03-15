using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using MediatR;

namespace DATN.Application.Features.Me.Commands;

/// <summary>Thêm địa chỉ mới vào sổ địa chỉ</summary>
public class AddAddressCommand : IRequest<ApiResponse<UserAddressDto>>
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string DetailedAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

/// <summary>Cập nhật địa chỉ đã có</summary>
public class UpdateAddressCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
    public string DetailedAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

/// <summary>Xóa địa chỉ (chỉ xóa của chính mình)</summary>
public record DeleteAddressCommand(Guid Id, Guid UserId) : IRequest<ApiResponse<bool>>;

/// <summary>Đặt địa chỉ là mặc định cho checkout</summary>
public record SetDefaultAddressCommand(Guid Id, Guid UserId) : IRequest<ApiResponse<bool>>;
