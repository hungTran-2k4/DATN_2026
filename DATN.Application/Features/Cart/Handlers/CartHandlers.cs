using DATN.Application.Common.Models;
using DATN.Application.DTOs.Cart;
using DATN.Application.Features.Cart.Commands;
using DATN.Application.Features.Cart.Queries;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace DATN.Application.Features.Cart.Handlers;

public class GetMyCartHandler : IRequestHandler<GetMyCartQuery, ApiResponse<CartDto>>
{
    private readonly ICartRepository _cartRepo;

    public GetMyCartHandler(ICartRepository cartRepo) => _cartRepo = cartRepo;

    public async Task<ApiResponse<CartDto>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
    {
        var items = await _cartRepo.GetByUserIdAsync(request.UserId, cancellationToken);

        // Group theo ShopId
        var groups = items
            .GroupBy(i => i.ShopId)
            .Select(g => new CartGroupDto
            {
                ShopId = g.Key ?? Guid.Empty,
                ShopName = g.FirstOrDefault()?.ProductName, // enriched by repo
                Items = g.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    VariantId = i.VariantId ?? Guid.Empty,
                    ShopId = i.ShopId ?? Guid.Empty,
                    ProductName = i.ProductName,
                    VariantName = i.VariantName,
                    VariantImageUrl = i.VariantImageUrl,
                    VariantAttributes = i.VariantAttributes != null
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(i.VariantAttributes)
                        : null,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    StockAvailable = 0 // filled by repo enrichment
                }).ToList()
            })
            .ToList();

        return ApiResponse<CartDto>.Succeed(new CartDto { Groups = groups });
    }
}

public class AddToCartHandler : IRequestHandler<AddToCartCommand, ApiResponse<CartItemDto>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductVariantRepository _variantRepo;

    public AddToCartHandler(ICartRepository cartRepo, IProductVariantRepository variantRepo)
    {
        _cartRepo = cartRepo;
        _variantRepo = variantRepo;
    }

    public async Task<ApiResponse<CartItemDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return ApiResponse<CartItemDto>.Fail("Số lượng phải lớn hơn 0.", 400, "INVALID_QUANTITY");

        // 1. Kiểm tra variant tồn tại và còn hàng
        var variant = await _variantRepo.GetByIdAsync(request.VariantId, cancellationToken);
        if (variant == null)
            return ApiResponse<CartItemDto>.Fail("Sản phẩm không tồn tại.", 404, "VARIANT_NOT_FOUND");

        var stock = await _variantRepo.GetStockQtyAsync(request.VariantId, cancellationToken);
        if (stock <= 0)
            return ApiResponse<CartItemDto>.Fail("Sản phẩm đã hết hàng.", 400, "OUT_OF_STOCK");

        // 2. Kiểm tra xem variant đã có trong giỏ chưa (upsert logic)
        var existing = await _cartRepo.GetByVariantIdAsync(request.UserId, request.VariantId, cancellationToken);

        CartItem result;
        if (existing != null)
        {
            var newQty = existing.Quantity + request.Quantity;
            if (newQty > stock)
                return ApiResponse<CartItemDto>.Fail($"Chỉ còn {stock} sản phẩm trong kho.", 400, "EXCEEDS_STOCK");

            await _cartRepo.UpdateQuantityAsync(existing.Id, request.UserId, newQty, cancellationToken);
            existing.Quantity = newQty;
            result = existing;
        }
        else
        {
            if (request.Quantity > stock)
                return ApiResponse<CartItemDto>.Fail($"Chỉ còn {stock} sản phẩm trong kho.", 400, "EXCEEDS_STOCK");

            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                UnitPrice = variant.Price,
                CreatedAt = DateTime.UtcNow
            };
            result = await _cartRepo.AddAsync(cartItem, cancellationToken);
        }

        return ApiResponse<CartItemDto>.Succeed(new CartItemDto
        {
            Id = result.Id,
            VariantId = result.VariantId ?? Guid.Empty,
            Quantity = result.Quantity,
            UnitPrice = result.UnitPrice,
            StockAvailable = stock
        }, "Đã thêm vào giỏ hàng.");
    }
}

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, ApiResponse<bool>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductVariantRepository _variantRepo;

    public UpdateCartItemHandler(ICartRepository cartRepo, IProductVariantRepository variantRepo)
    {
        _cartRepo = cartRepo;
        _variantRepo = variantRepo;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return ApiResponse<bool>.Fail("Số lượng phải lớn hơn 0. Dùng 'Xóa' để loại bỏ sản phẩm.", 400, "INVALID_QUANTITY");

        var item = await _cartRepo.GetByIdAsync(request.CartItemId, request.UserId, cancellationToken);
        if (item == null)
            return ApiResponse<bool>.Fail("Không tìm thấy item trong giỏ hàng.", 404, "CART_ITEM_NOT_FOUND");

        var stock = await _variantRepo.GetStockQtyAsync(item.VariantId!.Value, cancellationToken);
        if (request.Quantity > stock)
            return ApiResponse<bool>.Fail($"Chỉ còn {stock} sản phẩm trong kho.", 400, "EXCEEDS_STOCK");

        await _cartRepo.UpdateQuantityAsync(request.CartItemId, request.UserId, request.Quantity, cancellationToken);
        return ApiResponse<bool>.Succeed(true, "Đã cập nhật giỏ hàng.");
    }
}

public class RemoveCartItemHandler : IRequestHandler<RemoveCartItemCommand, ApiResponse<bool>>
{
    private readonly ICartRepository _cartRepo;
    public RemoveCartItemHandler(ICartRepository cartRepo) => _cartRepo = cartRepo;

    public async Task<ApiResponse<bool>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _cartRepo.RemoveAsync(request.CartItemId, request.UserId, cancellationToken);
        return result
            ? ApiResponse<bool>.Succeed(true, "Đã xóa sản phẩm khỏi giỏ hàng.")
            : ApiResponse<bool>.Fail("Không tìm thấy item.", 404, "CART_ITEM_NOT_FOUND");
    }
}

public class ClearCartHandler : IRequestHandler<ClearCartCommand, ApiResponse<bool>>
{
    private readonly ICartRepository _cartRepo;
    public ClearCartHandler(ICartRepository cartRepo) => _cartRepo = cartRepo;

    public async Task<ApiResponse<bool>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _cartRepo.ClearByUserIdAsync(request.UserId, cancellationToken);
        return ApiResponse<bool>.Succeed(true, "Đã xóa toàn bộ giỏ hàng.");
    }
}
