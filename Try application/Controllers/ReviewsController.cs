using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Try_application.Database;
using Try_application.Database.Entities;
using Try_application.Model;

namespace Try_application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ReviewsController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/product/5
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetProductReviews(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewDtos = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.UserName,
                Rating = r.Rating,
                Comment = r.Comment ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Ok(reviewDtos);
        }

        // POST: api/Reviews
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto reviewDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";

            // Check if user has purchased the product
            bool hasPurchased = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi =>
                    oi.Order != null &&
                    oi.Order.UserId == userId &&
                    oi.ProductId == reviewDto.ProductId &&
                    oi.Order.Status == "Claimed");

            if (!hasPurchased)
            {
                return BadRequest("You can only review products you have purchased");
            }

            // Check if user has already reviewed this product
            bool hasReviewed = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == reviewDto.ProductId);

            if (hasReviewed)
            {
                return BadRequest("You have already reviewed this product");
            }

            var review = new Review
            {
                ProductId = reviewDto.ProductId,
                UserId = userId,
                UserName = userName,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update product average rating
            var product = await _context.Products.FindAsync(reviewDto.ProductId);
            if (product != null)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == reviewDto.ProductId)
                    .ToListAsync();

                if (reviews.Any())
                {
                    product.Rating = reviews.Average(r => r.Rating);
                    await _context.SaveChangesAsync();
                }
            }

            var reviewDto2 = new ReviewDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                UserName = review.UserName,
                Rating = review.Rating,
                Comment = review.Comment ?? string.Empty,
                CreatedAt = review.CreatedAt
            };

            return CreatedAtAction(nameof(GetProductReviews), new { productId = review.ProductId }, reviewDto2);
        }

        // DELETE: api/Reviews/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var isAdmin = User.IsInRole("Admin");

            if (review.UserId != userId && !isAdmin)
            {
                return Forbid("You can only delete your own reviews");
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Update product average rating
            var product = await _context.Products.FindAsync(review.ProductId);
            if (product != null)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == review.ProductId)
                    .ToListAsync();

                product.Rating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
