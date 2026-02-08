using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Game.Queries;
using MyProject.Application.Interfaces.Games;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Handler;

public class GetGamesPagingHandler(
    IMapper mapper,
    IGameRepository repository,
    ILogger<GetGamesPagingHandler> logger
) : IRequestHandler<GetGamesPagingQuery, IEnumerable<GameBaseRespone>>
{

    public async Task<IEnumerable<GameBaseRespone>> Handle(GetGamesPagingQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetGamesPagingQuery request");

        var result = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<GameBaseRespone>>(result);


    }
}