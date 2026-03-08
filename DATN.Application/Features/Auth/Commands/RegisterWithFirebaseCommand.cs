using MediatR;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

public record RegisterWithFirebaseCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;
