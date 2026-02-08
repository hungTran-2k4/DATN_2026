using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Game.Commands;
using MyProject.Application.Interfaces.Games;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Handler;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, CreateGameRespone>
{
    private readonly IGameRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateGameCommandHandler> _logger;

    public CreateGameCommandHandler(
        IGameRepository repository,
        IMapper mapper,
        ILogger<CreateGameCommandHandler> logger
    )
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<CreateGameRespone> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateGameCommand request");

        var game = new Domain.Entities.Stores.Game
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            CoverImageUrl = request.CoverImageUrl,
            Price = request.Price,
            ShortDescription = request.ShortDescription,
            FullDescription = request.Description,
            PublisherId = request.PublisherId,
        };

        var createGame = await _repository.CreateAsync(game, cancellationToken);
        var respone = _mapper.Map<CreateGameRespone>(createGame);

        return respone;
    }
}
