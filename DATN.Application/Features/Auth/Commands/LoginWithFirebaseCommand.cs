using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

public record LoginWithFirebaseCommand(string Email, string Password) : IRequest<ApiResponse<AuthResponse>>;
