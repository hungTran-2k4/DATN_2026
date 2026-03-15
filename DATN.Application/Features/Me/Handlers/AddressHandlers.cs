using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Me.Handlers;

public class GetMyAddressesHandler : IRequestHandler<Queries.GetMyAddressesQuery, ApiResponse<IEnumerable<UserAddressDto>>>
{
    private readonly IUserAddressRepository _addressRepo;

    public GetMyAddressesHandler(IUserAddressRepository addressRepo) => _addressRepo = addressRepo;

    public async Task<ApiResponse<IEnumerable<UserAddressDto>>> Handle(Queries.GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        var addresses = await _addressRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        var dtos = addresses.Select(a => new UserAddressDto
        {
            Id = a.Id,
            FullName = a.FullName,
            PhoneNumber = a.PhoneNumber,
            ProvinceId = a.ProvinceId,
            DistrictId = a.DistrictId,
            WardId = a.WardId,
            DetailedAddress = a.DetailedAddress,
            IsDefault = a.IsDefault ?? false,
            CreatedAt = a.CreatedAt
        });
        return ApiResponse<IEnumerable<UserAddressDto>>.Succeed(dtos);
    }
}

public class AddAddressHandler : IRequestHandler<Commands.AddAddressCommand, ApiResponse<UserAddressDto>>
{
    private readonly IUserAddressRepository _addressRepo;

    public AddAddressHandler(IUserAddressRepository addressRepo) => _addressRepo = addressRepo;

    public async Task<ApiResponse<UserAddressDto>> Handle(Commands.AddAddressCommand request, CancellationToken cancellationToken)
    {
        // Nếu là địa chỉ đầu tiên hoặc yêu cầu là mặc định → cần set default
        var existing = await _addressRepo.GetByUserIdAsync(request.UserId, cancellationToken);
        var isFirstAddress = !existing.Any();

        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardId = request.WardId,
            DetailedAddress = request.DetailedAddress,
            IsDefault = isFirstAddress || request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        // Nếu user chọn đây là địa chỉ mặc định → bỏ default của các address khác trước
        if (request.IsDefault && !isFirstAddress)
        {
            // SetDefaultAsync sẽ bỏ default của tất cả rồi set cái mới
            // Nhưng address chưa được tạo → ta tự bỏ default các cái cũ bằng cách repo sẽ xử lý
        }

        var created = await _addressRepo.AddAsync(address, cancellationToken);

        // Nếu muốn set default, cần gọi sau khi có Id
        if ((isFirstAddress || request.IsDefault) && !isFirstAddress)
        {
            await _addressRepo.SetDefaultAsync(created.Id, request.UserId, cancellationToken);
        }

        return ApiResponse<UserAddressDto>.Succeed(new UserAddressDto
        {
            Id = created.Id,
            FullName = created.FullName,
            PhoneNumber = created.PhoneNumber,
            ProvinceId = created.ProvinceId,
            DistrictId = created.DistrictId,
            WardId = created.WardId,
            DetailedAddress = created.DetailedAddress,
            IsDefault = created.IsDefault ?? false,
            CreatedAt = created.CreatedAt
        }, "Thêm địa chỉ thành công.", 201);
    }
}

public class UpdateAddressHandler : IRequestHandler<Commands.UpdateAddressCommand, ApiResponse<bool>>
{
    private readonly IUserAddressRepository _addressRepo;

    public UpdateAddressHandler(IUserAddressRepository addressRepo) => _addressRepo = addressRepo;

    public async Task<ApiResponse<bool>> Handle(Commands.UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _addressRepo.GetByIdAsync(request.Id, request.UserId, cancellationToken);
        if (address == null)
            return ApiResponse<bool>.Fail("Không tìm thấy địa chỉ.", 404, "ADDRESS_NOT_FOUND");

        address.FullName = request.FullName;
        address.PhoneNumber = request.PhoneNumber;
        address.ProvinceId = request.ProvinceId;
        address.DistrictId = request.DistrictId;
        address.WardId = request.WardId;
        address.DetailedAddress = request.DetailedAddress;
        address.IsDefault = request.IsDefault;

        var result = await _addressRepo.UpdateAsync(address, cancellationToken);
        if (!result)
            return ApiResponse<bool>.Fail("Cập nhật thất bại.", 500);

        if (request.IsDefault)
            await _addressRepo.SetDefaultAsync(request.Id, request.UserId, cancellationToken);

        return ApiResponse<bool>.Succeed(true, "Cập nhật địa chỉ thành công.");
    }
}

public class DeleteAddressHandler : IRequestHandler<Commands.DeleteAddressCommand, ApiResponse<bool>>
{
    private readonly IUserAddressRepository _addressRepo;

    public DeleteAddressHandler(IUserAddressRepository addressRepo) => _addressRepo = addressRepo;

    public async Task<ApiResponse<bool>> Handle(Commands.DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _addressRepo.GetByIdAsync(request.Id, request.UserId, cancellationToken);
        if (address == null)
            return ApiResponse<bool>.Fail("Không tìm thấy địa chỉ.", 404, "ADDRESS_NOT_FOUND");

        if (address.IsDefault == true)
            return ApiResponse<bool>.Fail("Không thể xóa địa chỉ mặc định. Hãy đặt địa chỉ khác làm mặc định trước.", 400, "CANNOT_DELETE_DEFAULT");

        var result = await _addressRepo.DeleteAsync(request.Id, request.UserId, cancellationToken);
        return result
            ? ApiResponse<bool>.Succeed(true, "Xóa địa chỉ thành công.")
            : ApiResponse<bool>.Fail("Xóa thất bại.", 500);
    }
}

public class SetDefaultAddressHandler : IRequestHandler<Commands.SetDefaultAddressCommand, ApiResponse<bool>>
{
    private readonly IUserAddressRepository _addressRepo;

    public SetDefaultAddressHandler(IUserAddressRepository addressRepo) => _addressRepo = addressRepo;

    public async Task<ApiResponse<bool>> Handle(Commands.SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _addressRepo.GetByIdAsync(request.Id, request.UserId, cancellationToken);
        if (address == null)
            return ApiResponse<bool>.Fail("Không tìm thấy địa chỉ.", 404, "ADDRESS_NOT_FOUND");

        await _addressRepo.SetDefaultAsync(request.Id, request.UserId, cancellationToken);
        return ApiResponse<bool>.Succeed(true, "Đặt địa chỉ mặc định thành công.");
    }
}
