using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using MediatR;

namespace DATN.Application.Features.Me.Commands;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : IRequest<ApiResponse<bool>>;

public class UpdateProfileCommand : IRequest<ApiResponse<UserProfileDto>>
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
}
