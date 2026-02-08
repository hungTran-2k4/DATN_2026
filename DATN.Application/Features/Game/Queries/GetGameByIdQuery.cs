using MediatR;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Queries;

public record GetGameByIdQuery(Guid Id) : IRequest<GetGameByIdRespone>;