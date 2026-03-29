using MediatR;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ApiResponse<string>>;
