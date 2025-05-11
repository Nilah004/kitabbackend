using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Try_application.Database.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string PostalCode { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string ClaimCode { get; set; }

        public bool IsCancelled { get; set; } = false;

        public DateTime? CancelledDate { get; set; }

        public DateTime? ClaimedDate { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
