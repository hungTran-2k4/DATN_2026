using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using MediatR;

namespace DATN.Application.Features.Me.Queries;

public record GetMyProfileQuery(Guid UserId) : IRequest<ApiResponse<UserProfileDto>>;

public record GetMyAddressesQuery(Guid UserId) : IRequest<ApiResponse<IEnumerable<UserAddressDto>>>;
