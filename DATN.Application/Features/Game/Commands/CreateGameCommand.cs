using MediatR;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Commands;

public record CreateGameCommand(
    string Title,
    string Slug,
    string? ShortDescription,
    string Description,
    decimal Price,
    string? CoverImageUrl,
    Guid PublisherId,
    DateTime ReleaseDate
) : IRequest<CreateGameRespone>;