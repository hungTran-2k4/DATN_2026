using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyProject.Application.Models.Store
{
    public record GameBaseRespone
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        //public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string FullDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        //public decimal? FinalPrice { get; set; }
        //public int? DiscountPercent { get; set; }
        //public bool? IsOnSale { get; set; }
        public string? CoverImage { get; set; }
        public string? status { get; set; }
        public string? ReleaseDate { get; set; }
        public Guid PublisherId { get; set; }

        // Nested DTO
        public string? PublisherName { get; set; }
    }

    //game command respone
    public record CreateGameRespone : GameBaseRespone;
    public record UpdateGameRespone : GameBaseRespone;
    public record DeleteGameRespone : GameBaseRespone;

    //game query respone
    public record GetGameByIdRespone : GameBaseRespone;
    public record GetGameBySlugRespone : GameBaseRespone;
    public record GetAllGamesRespone : GameBaseRespone;
    public record GetGameByPublisherRespone : GameBaseRespone;

    // alias
    public record GameDto : GameBaseRespone;

    //game request
    public class CreateGameRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string FullDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? FinalPrice { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public Guid PublisherId { get; set; }

    }
    public class UpdateGameRequest
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string FullDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? FinalPrice { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public Guid PublisherId { get; set; }
    }
}