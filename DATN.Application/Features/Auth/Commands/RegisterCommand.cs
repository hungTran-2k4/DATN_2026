using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

/// <summary>
/// MediatR Command cho đăng ký tài khoản
/// </summary>
public record RegisterCommand(
    string Email, 
    string Password, 
    string? FullName,
    string? Username
) : IRequest<ApiResponse<AuthResponse>>;
