using MediatR;
using DATN.Application.Features.Users.Queries;
using DATN.Application.DTOs.Users;
using DATN.Application.Common.Models;
using DATN.Domain.Interfaces;
using DATN.Domain.Extensions;

namespace DATN.Application.Features.Users.Handlers;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithDetailsAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDetailDto>.Fail("Không tìm thấy người dùng", 404, "USER_NOT_FOUND");
        }

        var dto = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Status = user.AccountStatus.ToDatabaseString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            FailedLoginCount = user.FailedLoginCount,
            LockoutEnd = user.LockoutEnd
        };

        // Map roles
        if (user.UserRoles != null)
        {
            dto.Roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role.Name)
                .ToList();
        }

        return ApiResponse<UserDetailDto>.Succeed(dto, "Lấy thông tin người dùng thành công");
    }
}
