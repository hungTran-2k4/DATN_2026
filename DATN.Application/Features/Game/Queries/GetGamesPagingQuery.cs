using MediatR;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Queries;

public record GetGamesPagingQuery : IRequest<IEnumerable<GameBaseRespone>>;