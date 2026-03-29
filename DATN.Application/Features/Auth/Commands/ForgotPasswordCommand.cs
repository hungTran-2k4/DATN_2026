using MediatR;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email, string? IpAddress) : IRequest<ApiResponse<string>>;
