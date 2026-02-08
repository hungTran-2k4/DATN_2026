using MediatR;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Features.Game.Queries;

public record GetGameBySlugQuery(string Slug) : IRequest<GetGameBySlugRespone>;