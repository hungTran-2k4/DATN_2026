using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

public record RegisterWithFirebaseCommand(string Email, string Password, string FullName) : IRequest<ApiResponse<AuthResponse>>;
