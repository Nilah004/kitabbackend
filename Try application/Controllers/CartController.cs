using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Try_application.Database;
using Try_application.Database.Entities;
using Try_application.Model;

namespace Try_application.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDBContext _context;

        public CartController(AppDBContext context)
        {
            _context = context;
        }

        // ✅ GET: api/Cart?userId=xyz
        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var cartDto = new CartDto
            {
                Items = cartItems.Select(c => new CartItemDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    Product = c.Product != null ? new ProductDto
                    {
                        Id = c.Product.Id,
                        Name = c.Product.Name,
                        Description = c.Product.Description ?? string.Empty,
                        Price = c.Product.Price,
                        Image = c.Product.Image ?? string.Empty
                    } : null,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product?.Price ?? 0,
                    Subtotal = c.Quantity * (c.Product?.Price ?? 0)
                }).ToList(),
                Subtotal = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0)),
                Total = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0)),
                ItemCount = cartItems.Sum(c => c.Quantity)
            };

            return Ok(cartDto);
        }

        // ✅ POST: api/Cart?userId=xyz
        [HttpPost]
        public async Task<ActionResult> AddToCart(string userId, [FromBody] AddToCartDto dto)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            if (dto == null)
            {
                return BadRequest("Invalid request data");
            }

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
            }
            else
            {
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return NotFound("Product not found");

                var newItem = new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    DateAdded = DateTime.UtcNow
                };

                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ✅ PUT: api/Cart/{id}?userId=xyz
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateQuantity(int id, string userId, [FromBody] UpdateCartDto dto)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            if (dto == null)
            {
                return BadRequest("Invalid request data");
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (item == null)
                return NotFound();

            item.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ✅ DELETE: api/Cart/{id}?userId=xyz
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveItem(int id, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (item == null)
                return NotFound();

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ✅ DELETE: api/Cart?userId=xyz
        [HttpDelete]
        public async Task<ActionResult> ClearCart(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var items = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
