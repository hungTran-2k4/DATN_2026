namespace DATN.Domain.Entities.Products;

/// <summary>
/// Danh sách yêu thích của người dùng (Wishlist/Favorite)
/// </summary>
public class WishlistItem
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime? CreatedAt { get; set; }
}
