using System;
using System.ComponentModel.DataAnnotations;

namespace Try_application.Database.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? DiscountedPrice { get; set; }

        public bool OnSale { get; set; }

        public string? Image { get; set; }

        public string? Author { get; set; }

        public string? Publisher { get; set; }

        public int? PublicationYear { get; set; }

        public string? ISBN { get; set; }

        public int? PageCount { get; set; }

        public string? Language { get; set; }

        // Change from Category to Genre if that's what exists in the database
        // public string? Category { get; set; }
        public string? Genre { get; set; }

        public string? Section { get; set; }

        public double Rating { get; set; }

        public int StockQuantity { get; set; }

        // Previously added properties
        public DateTime? DiscountEndDate { get; set; }

        public bool IsAvailableInStore { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsBestseller { get; set; }

        public bool IsNewRelease { get; set; }

        public bool IsComingSoon { get; set; }

        public decimal? DiscountPercent { get; set; }

        public DateTime? DiscountStartDate { get; set; }

        public int? Pages { get; set; }

        public string? Dimensions { get; set; }

        public decimal? Weight { get; set; }

        public DateTime? PublicationDate { get; set; }

        public bool IsAwardWinner { get; set; }

        // New properties from the latest error messages
        public int TotalSold { get; set; }

        public string? Format { get; set; }
    }
}
