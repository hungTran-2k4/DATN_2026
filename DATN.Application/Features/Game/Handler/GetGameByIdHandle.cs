using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Game.Queries;
using MyProject.Application.Interfaces.Games;
using MyProject.Application.Models.Store;
using MyProject.Domain.Entities.Stores;

namespace MyProject.Application.Features.Game.Handler
{
    public class GetGameByIdHandle(
        IGameRepository repo,
        IMapper mapper,
        ILogger<GetGameByIdHandle> logger
        ) : IRequestHandler<GetGameByIdQuery, GetGameByIdRespone>
    {
        public async Task<GetGameByIdRespone> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var game = await repo.GetByIdAsync(request.Id, cancellationToken);
                return mapper.Map<GetGameByIdRespone>(game);

            }
            catch (Exception ex)
            {
                return mapper.Map<GetGameByIdRespone>(null);
            }
        }
    }
}
