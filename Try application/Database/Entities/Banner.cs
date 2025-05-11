using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Try_application.Database.Entities
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? SubTitle { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Mark this property as not mapped to the database
        [NotMapped]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
