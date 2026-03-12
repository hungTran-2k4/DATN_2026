using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

/// <summary>
/// MediatR Command cho đăng nhập
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<ApiResponse<AuthResponse>>;
