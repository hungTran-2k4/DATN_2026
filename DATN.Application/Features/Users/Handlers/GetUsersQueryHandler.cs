using MediatR;
using AutoMapper;
using DATN.Application.Features.Users.Queries;
using DATN.Domain.Interfaces;
using DATN.Application.DTOs.Auth;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Users.Handlers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<IEnumerable<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _userRepository.GetPagedAsync(request.Search, request.Filter, request.Page, request.PageSize, cancellationToken);
        var userDtos = new List<UserDto>();

        foreach (var user in items)
        {
            var dto = _mapper.Map<UserDto>(user);
            
            // Roles are already fetched via PrefetchPath in Repository
            // Extract Role names from User -> UserRole -> Role
            if (user.UserRoles != null)
            {
                dto.Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .ToList();
            }
            
            userDtos.Add(dto);
        }

        return PagedResponse<IEnumerable<UserDto>>.SucceedDefault(userDtos, request.Page, request.PageSize, total);
    }
}
