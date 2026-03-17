using DATN.Application.Common.Models;
using MediatR;

namespace DATN.Application.Features.Me.Commands;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : IRequest<ApiResponse<bool>>;

