using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Try_application.Database;
using Try_application.Database.Entities;
using Try_application.Model;

namespace Try_application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookMarkController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly ILogger<BookMarkController> _logger;

        public BookMarkController(AppDBContext context, ILogger<BookMarkController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /api/BookMark?userId=abc123
        [HttpGet]
        public async Task<IActionResult> GetBookmarks([FromQuery] string userId)
        {
            try
            {
                _logger.LogInformation($"GetBookmarks called for userId: {userId}");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("GetBookmarks: UserId is empty");
                    return BadRequest("UserId is required.");
                }

                var bookmarks = await _context.Bookmarks
                    .Include(b => b.Product)
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                _logger.LogInformation($"Found {bookmarks.Count} bookmarks for userId: {userId}");

                var result = bookmarks.Select(b => new
                {
                    b.Id,
                    b.UserId,
                    b.ProductId,
                    Product = b.Product != null ? new
                    {
                        b.Product.Id,
                        b.Product.Name,
                        b.Product.Description,
                        b.Product.Price,
                        b.Product.Image,
                        b.Product.Author,
                        b.Product.Publisher,
                        b.Product.OnSale,
                        DiscountPercent = b.Product.DiscountPercent,
                        FinalPrice = b.Product.OnSale && b.Product.DiscountPercent.HasValue
                            ? b.Product.Price * (1 - b.Product.DiscountPercent.Value / 100)
                            : b.Product.Price
                    } : null,
                    DateAdded = b.DateAdded
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBookmarks for userId: {userId}");
                return StatusCode(500, new { message = "Error retrieving bookmarks", error = ex.Message });
            }
        }

        // POST: /api/BookMark?userId=abc123
        [HttpPost]
        public async Task<IActionResult> AddBookmark([FromQuery] string userId, [FromBody] AddBookmarkDto dto)
        {
            try
            {
                _logger.LogInformation($"AddBookmark called for userId: {userId}, productId: {dto?.ProductId}");

                if (dto == null || dto.ProductId <= 0 || string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning($"AddBookmark: Invalid request data. UserId: {userId}, ProductId: {dto?.ProductId}");
                    return BadRequest("Invalid request data.");
                }

                // Check if product exists
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    _logger.LogWarning($"AddBookmark: Product not found. ProductId: {dto.ProductId}");
                    return NotFound("Product not found.");
                }

                // Prevent duplicates
                bool exists = await _context.Bookmarks.AnyAsync(b => b.UserId == userId && b.ProductId == dto.ProductId);
                if (exists)
                {
                    _logger.LogInformation($"AddBookmark: Bookmark already exists. UserId: {userId}, ProductId: {dto.ProductId}");
                    return Conflict("Bookmark already exists.");
                }

                var bookmark = new Bookmark
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    CreatedAt = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow
                };

                _context.Bookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bookmark added successfully. Id: {bookmark.Id}");
                return Ok(bookmark);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AddBookmark for userId: {userId}, productId: {dto?.ProductId}");
                return StatusCode(500, new { message = "Error adding bookmark", error = ex.Message });
            }
        }

        // DELETE: /api/BookMark/{productId}?userId=abc123
        [HttpDelete("{productId}")]
        public async Task<IActionResult> DeleteBookmark(int productId, [FromQuery] string userId)
        {
            try
            {
                _logger.LogInformation($"DeleteBookmark called for userId: {userId}, productId: {productId}");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("DeleteBookmark: UserId is empty");
                    return BadRequest("UserId is required.");
                }

                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ProductId == productId);

                if (bookmark == null)
                {
                    _logger.LogWarning($"DeleteBookmark: Bookmark not found. UserId: {userId}, ProductId: {productId}");
                    return NotFound("Bookmark not found.");
                }

                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bookmark deleted successfully. UserId: {userId}, ProductId: {productId}");
                return Ok(new { message = "Bookmark removed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteBookmark for userId: {userId}, productId: {productId}");
                return StatusCode(500, new { message = "Error removing bookmark", error = ex.Message });
            }
        }
    }
}
