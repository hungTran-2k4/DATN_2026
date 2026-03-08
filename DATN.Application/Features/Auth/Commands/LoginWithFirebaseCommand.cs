using MediatR;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

public record LoginWithFirebaseCommand(string Email, string Password) : IRequest<AuthResponse>;
