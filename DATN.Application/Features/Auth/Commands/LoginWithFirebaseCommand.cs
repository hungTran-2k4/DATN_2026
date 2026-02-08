using MediatR;
using MyProject.Application.Models.Auth;

namespace MyProject.Application.Features.Auth.Commands;

public record LoginWithFirebaseCommand(string Email, string Password) : IRequest<AuthResponse>;
