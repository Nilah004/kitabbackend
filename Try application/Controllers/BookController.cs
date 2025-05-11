using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Try_application.Database;
using Try_application.Database.Entities;
using Try_application.Model;

namespace Try_application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult GetProducts(
            int page = 1,
            int limit = 10,
            string? category = null,
            string? search = null,
            string? genre = null,
            decimal? maxPrice = null,
            string? sort = "name")
        {
            Console.WriteLine($"API Request - Page: {page}, Limit: {limit}, Category: {category}, Search: {search}, Genre: {genre}, MaxPrice: {maxPrice}, Sort: {sort}");

            var query = _context.Products.AsQueryable();
            var now = DateTime.UtcNow;
            var threeMonthsAgo = now.AddMonths(-3);
            var oneMonthAgo = now.AddMonths(-1);

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                Console.WriteLine($"Filtering by category: {category}");

                switch (category.ToLower())
                {
                    case "bestsellers":
                        query = query.Where(p => p.IsBestseller);
                        Console.WriteLine("Applied bestsellers filter");
                        break;
                    case "award-winners":
                        query = query.Where(p => p.IsAwardWinner);
                        Console.WriteLine("Applied award winners filter");
                        break;
                    case "new-releases":
                        query = query.Where(p => p.IsNewRelease ||
                                      (p.PublicationDate >= threeMonthsAgo &&
                                       p.PublicationDate <= now));
                        Console.WriteLine("Applied new releases filter");
                        break;
                    case "new-arrivals":
                        query = query.Where(p => p.CreatedAt >= oneMonthAgo);
                        Console.WriteLine("Applied new arrivals filter");
                        break;
                    case "coming-soon":
                        query = query.Where(p => p.IsComingSoon);
                        Console.WriteLine("Applied coming soon filter");
                        break;
                    case "deals":
                        query = query.Where(p => p.OnSale && p.DiscountPercent > 0);
                        Console.WriteLine("Applied deals filter");
                        break;
                    default:
                        Console.WriteLine($"Unknown category: {category}");
                        break;
                }
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    (p.Name != null && p.Name.Contains(search)) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    (p.Author != null && p.Author.Contains(search)) ||
                    (p.ISBN != null && p.ISBN.Contains(search))
                );
                Console.WriteLine($"Applied search filter: {search}");
            }

            // Apply genre filter
            if (!string.IsNullOrEmpty(genre) && genre.ToLower() != "all")
            {
                query = query.Where(p => p.Genre == genre);
                Console.WriteLine($"Applied genre filter: {genre}");
            }

            // Apply price filter
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
                Console.WriteLine($"Applied max price filter: {maxPrice}");
            }

            // Get total count before pagination (but after all filters)
            var totalItems = query.Count();
            Console.WriteLine($"Total items after filtering: {totalItems}");

            // Apply sorting
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "popularity" => query.OrderByDescending(p => p.TotalSold),
                "newest" => query.OrderByDescending(p => p.PublicationDate),
                _ => query.OrderBy(p => p.Name)
            };

            // Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalItems / limit);
            Console.WriteLine($"Total pages: {totalPages}");

            // Apply pagination
            var pagedQuery = query.Skip((page - 1) * limit).Take(limit);

            var products = pagedQuery
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    image = p.Image,
                    price = p.Price,
                    author = p.Author,
                    genre = p.Genre,
                    publisher = p.Publisher,
                    publicationDate = p.PublicationDate,
                    isAwardWinner = p.IsAwardWinner,
                    isBestseller = p.IsBestseller,
                    isNewRelease = p.IsNewRelease,
                    isComingSoon = p.IsComingSoon,
                    discountPercent = p.DiscountPercent,
                    discountStartDate = p.DiscountStartDate,
                    discountEndDate = p.DiscountEndDate,
                    onSale = p.OnSale,
                    finalPrice = (p.OnSale &&
                          p.DiscountPercent.HasValue &&
                          (!p.DiscountStartDate.HasValue || p.DiscountStartDate <= now) &&
                          (!p.DiscountEndDate.HasValue || p.DiscountEndDate >= now))
                         ? p.Price * (1 - p.DiscountPercent.Value / 100)
                         : p.Price,
                    totalSold = p.TotalSold,
                    createdAt = p.CreatedAt
                })
                .ToList();

            Console.WriteLine($"Returning {products.Count} products for page {page}");

            return Ok(new
            {
                total = totalItems,
                page,
                limit,
                totalPages,
                category,
                data = products
            });
        }

        // Other methods remain the same...

        // ✅ GET single product
        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound(new { message = "Product not found." });

            var now = DateTime.UtcNow;
            var finalPrice = (product.OnSale &&
                              product.DiscountPercent.HasValue &&
                              (!product.DiscountStartDate.HasValue || product.DiscountStartDate <= now) &&
                              (!product.DiscountEndDate.HasValue || product.DiscountEndDate >= now))
                             ? product.Price * (1 - product.DiscountPercent.Value / 100)
                             : product.Price;

            // Calculate days left for sale
            int? daysLeft = null;
            if (product.OnSale && product.DiscountEndDate.HasValue && product.DiscountEndDate > now)
            {
                daysLeft = (int)(product.DiscountEndDate.Value - now).TotalDays;
            }

            var result = new
            {
                id = product.Id,
                name = product.Name,
                description = product.Description,
                image = product.Image,
                price = product.Price,
                author = product.Author,
                genre = product.Genre,
                publisher = product.Publisher,
                isbn = product.ISBN,
                language = product.Language,
                format = product.Format,
                pages = product.Pages,
                dimensions = product.Dimensions,
                weight = product.Weight,
                publicationDate = product.PublicationDate,
                isAwardWinner = product.IsAwardWinner,
                isBestseller = product.IsBestseller,
                isNewRelease = product.IsNewRelease,
                isComingSoon = product.IsComingSoon,
                discountPercent = product.DiscountPercent,
                discountStartDate = product.DiscountStartDate,
                discountEndDate = product.DiscountEndDate,
                daysLeft = daysLeft,
                onSale = product.OnSale,
                finalPrice = finalPrice,
                stockQuantity = product.StockQuantity,
                isAvailableInStore = product.IsAvailableInStore,
                totalSold = product.TotalSold,
                rating = product.Rating,
                createdAt = product.CreatedAt
            };

            return Ok(result);
        }

        // ✅ SEARCH
        [HttpGet("search")]
        public IActionResult SearchProducts(
            string? q,
            string? genre = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = "name")
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(p =>
                    (p.Name != null && p.Name.Contains(q)) ||
                    (p.Description != null && p.Description.Contains(q)) ||
                    (p.Author != null && p.Author.Contains(q)) ||
                    (p.Genre != null && p.Genre.Contains(q)) ||
                    (p.ISBN != null && p.ISBN.Contains(q))
                );
            }

            if (!string.IsNullOrEmpty(genre) && genre.ToLower() != "all")
            {
                query = query.Where(p => p.Genre == genre);
            }

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "popularity" => query.OrderByDescending(p => p.TotalSold),
                "newest" => query.OrderByDescending(p => p.PublicationDate),
                _ => query.OrderBy(p => p.Name)
            };

            return Ok(query.ToList());
        }

        // ✅ ADD PRODUCT
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromForm] ProductDto dto, IFormFile image)
        {
            if (dto == null || image == null)
                return BadRequest(new { message = "Product or image is missing." });

            try
            {
                if (string.IsNullOrEmpty(_env.WebRootPath))
                    return StatusCode(500, new { message = "WebRootPath is not configured." });

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Parse categories if provided
                List<string> categories = new List<string>();
                if (!string.IsNullOrEmpty(dto.Categories))
                {
                    try
                    {
                        categories = JsonSerializer.Deserialize<List<string>>(dto.Categories);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing categories: {ex.Message}");
                    }
                }

                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Author = dto.Author,
                    Genre = dto.Genre,
                    Publisher = dto.Publisher,
                    ISBN = dto.ISBN,
                    Language = dto.Language,
                    Format = dto.Format,
                    Pages = dto.Pages,
                    Dimensions = dto.Dimensions,
                    Weight = dto.Weight,
                    PublicationDate = dto.PublicationDate?.ToUniversalTime(),
                    IsAwardWinner = dto.IsAwardWinner || categories.Contains("award-winners"),
                    IsBestseller = dto.IsBestseller || categories.Contains("bestsellers"),
                    IsNewRelease = dto.IsNewRelease || categories.Contains("new-releases"),
                    IsComingSoon = dto.IsComingSoon || categories.Contains("coming-soon"),
                    DiscountPercent = dto.DiscountPercent,
                    DiscountStartDate = dto.DiscountStartDate?.ToUniversalTime(),
                    DiscountEndDate = dto.DiscountEndDate?.ToUniversalTime(),
                    OnSale = dto.OnSale || categories.Contains("deals"),
                    StockQuantity = dto.StockQuantity,
                    IsAvailableInStore = dto.IsAvailableInStore,
                    Image = $"/uploads/{fileName}",
                    TotalSold = 0,
                    Rating = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding product.", error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ✅ UPDATE PRODUCT
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto dto, IFormFile? image)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound(new { message = "Product not found." });

            try
            {
                if (image != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    product.Image = $"/uploads/{fileName}";
                }

                // Parse categories if provided
                List<string> categories = new List<string>();
                if (!string.IsNullOrEmpty(dto.Categories))
                {
                    try
                    {
                        categories = JsonSerializer.Deserialize<List<string>>(dto.Categories);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing categories: {ex.Message}");
                    }
                }

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.Author = dto.Author;
                product.Genre = dto.Genre;
                product.Publisher = dto.Publisher;
                product.ISBN = dto.ISBN;
                product.Language = dto.Language;
                product.Format = dto.Format;
                product.Pages = dto.Pages;
                product.Dimensions = dto.Dimensions;
                product.Weight = dto.Weight;
                product.PublicationDate = dto.PublicationDate?.ToUniversalTime();
                product.IsAwardWinner = dto.IsAwardWinner || categories.Contains("award-winners");
                product.IsBestseller = dto.IsBestseller || categories.Contains("bestsellers");
                product.IsNewRelease = dto.IsNewRelease || categories.Contains("new-releases");
                product.IsComingSoon = dto.IsComingSoon || categories.Contains("coming-soon");
                product.DiscountPercent = dto.DiscountPercent;
                product.DiscountStartDate = dto.DiscountStartDate?.ToUniversalTime();
                product.DiscountEndDate = dto.DiscountEndDate?.ToUniversalTime();
                product.OnSale = dto.OnSale || categories.Contains("deals");
                product.StockQuantity = dto.StockQuantity;
                product.IsAvailableInStore = dto.IsAvailableInStore;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating product.", error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ✅ DELETE PRODUCT
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound(new { message = "Product not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product deleted." });
        }
    }
}
