using System;
using System.Collections.Generic;

namespace Try_application.Model
{
    public class ProductDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Image { get; set; } = string.Empty;

        // Book-specific properties
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;

        // Properties missing from the original ProductDto
        public string ISBN { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Categories { get; set; } = string.Empty;
        public int? Pages { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;

        // Date properties
        public DateTime? PublicationDate { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        // Boolean properties
        public bool IsAwardWinner { get; set; }
        public bool IsBestseller { get; set; }
        public bool IsNewRelease { get; set; }
        public bool IsComingSoon { get; set; }
        public bool OnSale { get; set; }
        public bool IsAvailableInStore { get; set; }

        // Numeric properties
        public decimal? DiscountPercent { get; set; }
        public decimal? FinalPrice { get; set; }
        public int StockQuantity { get; set; }
        public int TotalSold { get; set; }
        public decimal Rating { get; set; }
    }
}
