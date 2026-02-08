namespace MyProject.Domain.Entities.Stores;

public class Game
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public decimal Price { get; set; }
    //public decimal? FinalPrice { get; set; }
    //public int? DiscountPercent { get; set; }
    //public bool? IsOnSale { get; set; }
    public string? CoverImageUrl { get; set; }
    //public string? TrailerUrl { get; set; }
    public string? status { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public Guid PublisherId { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties (optional)
    public Publisher? Publisher { get; set; }
}