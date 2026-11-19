using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.DTOs;
using FUNewsManagement_CoreAPI.Models;
using FUNewsManagement_CoreAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FUNewsManagement_CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly FUNewsDbContext _context;
        private readonly IAuditService _auditService;

        public CategoryController(FUNewsDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /api/category
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? onlyActive)
        {
            var query = _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.NewsArticles)
                .AsQueryable();

            if (onlyActive.HasValue && onlyActive.Value)
            {
                query = query.Where(c => c.IsActive);
            }

            var list = await query
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryDto
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    CategoryDescription = c.CategoryDescription,
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                    IsActive = c.IsActive,
                    ArticleCount = c.NewsArticles.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: /api/category/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.NewsArticles)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = $"Không tìm thấy chuyên mục #{id}." });

            return Ok(new CategoryDto
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                CategoryDescription = category.CategoryDescription,
                ParentCategoryID = category.ParentCategoryID,
                ParentCategoryName = category.ParentCategory != null ? category.ParentCategory.CategoryName : null,
                IsActive = category.IsActive,
                ArticleCount = category.NewsArticles.Count
            });
        }

        // POST: /api/category
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = new Category
            {
                CategoryName = dto.CategoryName,
                CategoryDescription = dto.CategoryDescription,
                ParentCategoryID = dto.ParentCategoryID > 0 ? dto.ParentCategoryID : null,
                IsActive = dto.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "CREATE",
                "Category",
                category.CategoryID.ToString(),
                null,
                new { category.CategoryName, category.IsActive }
            );

            return CreatedAtAction(nameof(GetById), new { id = category.CategoryID }, new { id = category.CategoryID, message = "Tạo chuyên mục thành công!" });
        }

        // PUT: /api/category/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = $"Không tìm thấy chuyên mục #{id}." });

            var beforeData = new { category.CategoryName, category.CategoryDescription, category.IsActive, category.ParentCategoryID };

            category.CategoryName = dto.CategoryName;
            category.CategoryDescription = dto.CategoryDescription;
            category.ParentCategoryID = dto.ParentCategoryID > 0 ? dto.ParentCategoryID : null;
            category.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "UPDATE",
                "Category",
                id.ToString(),
                beforeData,
                new { dto.CategoryName, dto.CategoryDescription, dto.IsActive }
            );

            return Ok(new { message = "Cập nhật chuyên mục thành công!", id = category.CategoryID });
        }

        // PUT: /api/category/{id}/toggle-active
        [HttpPut("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = $"Không tìm thấy chuyên mục #{id}." });

            var oldStatus = category.IsActive;
            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "UPDATE",
                "Category",
                id.ToString(),
                new { IsActive = oldStatus },
                new { IsActive = category.IsActive }
            );

            return Ok(new { message = $"Trạng thái chuyên mục đã chuyển thành {(category.IsActive ? "Kích hoạt" : "Vô hiệu hóa")}.", isActive = category.IsActive });
        }

        // DELETE: /api/category/{id}
        // Enforce Business Rule: Prevent deletion if category contains articles!
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.NewsArticles)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = $"Không tìm thấy chuyên mục #{id}." });

            if (category.NewsArticles.Any())
            {
                return BadRequest(new
                {
                    message = $"Không thể xóa chuyên mục '{category.CategoryName}' vì đang có {category.NewsArticles.Count} bài viết trực thuộc. Bạn có thể chuyển bài viết sang chuyên mục khác hoặc chọn vô hiệu hóa chuyên mục."
                });
            }

            var beforeData = new { category.CategoryName, category.CategoryDescription, category.IsActive };

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "DELETE",
                "Category",
                id.ToString(),
                beforeData,
                null
            );

            return Ok(new { message = $"Đã xóa thành công chuyên mục '{category.CategoryName}'." });
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        private string? GetCurrentUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    }
}
