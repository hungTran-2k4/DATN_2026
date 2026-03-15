using DATN.Application.Common.Models;
using DATN.Application.DTOs.Cart;
using MediatR;

namespace DATN.Application.Features.Cart.Queries;

public record GetMyCartQuery(Guid UserId) : IRequest<ApiResponse<CartDto>>;
