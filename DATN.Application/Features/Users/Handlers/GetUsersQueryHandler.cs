using MediatR;
using AutoMapper;
using MyProject.Application.Features.Users.Queries;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;

namespace MyProject.Application.Features.Users.Handlers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
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

        return userDtos;
    }
}
