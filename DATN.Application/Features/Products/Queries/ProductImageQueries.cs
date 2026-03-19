using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Products.Queries;

public record GetProductImagesQuery(Guid ProductId) : IRequest<ApiResponse<IEnumerable<ProductImageDto>>>;
