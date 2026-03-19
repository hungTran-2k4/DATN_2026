using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using DATN.Application.Features.Me.Commands;
using DATN.Application.Features.Me.Queries;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Me.Handlers;

public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, ApiResponse<UserProfileDto>>
{
    private readonly IUserRepository _userRepo;

    public GetMyProfileHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<ApiResponse<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng.", 404, "USER_NOT_FOUND");

        return ApiResponse<UserProfileDto>.Succeed(new UserProfileDto
        {
            Id = user.Id,
            Username = user.FullName ?? user.Email,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Status = user.IsActive ? "active" : "inactive",
            CreatedAt = user.CreatedAt
        });
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(IUserRepository userRepo, IPasswordHasher passwordHasher)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return ApiResponse<bool>.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.", 400, "WEAK_PASSWORD");

        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<bool>.Fail("Không tìm thấy người dùng.", 404, "USER_NOT_FOUND");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<bool>.Fail("Mật khẩu hiện tại không đúng.", 400, "INVALID_CURRENT_PASSWORD");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, cancellationToken);
        return ApiResponse<bool>.Succeed(true, "Đổi mật khẩu thành công.");
    }
}

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, ApiResponse<UserProfileDto>>
{
    private readonly IUserRepository _userRepo;

    public UpdateProfileHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<ApiResponse<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng.", 404, "USER_NOT_FOUND");

        if (request.FullName != null)
            user.FullName = request.FullName;
        if (request.AvatarUrl != null)
            user.AvatarUrl = request.AvatarUrl;

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);

        return ApiResponse<UserProfileDto>.Succeed(new UserProfileDto
        {
            Id = user.Id,
            Username = user.FullName ?? user.Email,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Status = user.IsActive ? "active" : "inactive",
            CreatedAt = user.CreatedAt
        }, "Cập nhật profile thành công.");
    }
}
