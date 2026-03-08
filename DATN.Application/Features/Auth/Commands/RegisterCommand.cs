using MediatR;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

/// <summary>
/// MediatR Command cho đăng ký tài khoản
/// </summary>
public record RegisterCommand(
    string Email, 
    string Password, 
    string? FullName
) : IRequest<AuthResponse>;
