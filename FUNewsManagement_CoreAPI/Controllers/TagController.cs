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
    public class TagController : ControllerBase
    {
        private readonly FUNewsDbContext _context;
        private readonly IAuditService _auditService;

        public TagController(FUNewsDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /api/tag
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _context.Tags
                .Include(t => t.NewsTags)
                .OrderBy(t => t.TagName)
                .Select(t => new TagDto
                {
                    TagID = t.TagID,
                    TagName = t.TagName,
                    Note = t.Note,
                    ArticleCount = t.NewsTags.Count
                })
                .ToListAsync();

            return Ok(tags);
        }

        // GET: /api/tag/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tag = await _context.Tags
                .Include(t => t.NewsTags)
                .FirstOrDefaultAsync(t => t.TagID == id);

            if (tag == null)
                return NotFound(new { message = $"Không tìm thấy thẻ tag #{id}." });

            return Ok(new TagDto
            {
                TagID = tag.TagID,
                TagName = tag.TagName,
                Note = tag.Note,
                ArticleCount = tag.NewsTags.Count
            });
        }

        // GET: /api/tag/search?q=...
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<TagDto>());

            var term = q.Trim().ToLower();
            var tags = await _context.Tags
                .Include(t => t.NewsTags)
                .Where(t => t.TagName.ToLower().Contains(term) || (t.Note != null && t.Note.ToLower().Contains(term)))
                .Select(t => new TagDto
                {
                    TagID = t.TagID,
                    TagName = t.TagName,
                    Note = t.Note,
                    ArticleCount = t.NewsTags.Count
                })
                .ToListAsync();

            return Ok(tags);
        }

        // GET: /api/tag/{id}/articles
        [HttpGet("{id}/articles")]
        public async Task<IActionResult> GetArticlesByTag(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
                return NotFound(new { message = $"Không tìm thấy thẻ tag #{id}." });

            var articles = await _context.NewsTags
                .Where(nt => nt.TagID == id)
                .Include(nt => nt.NewsArticle)
                    .ThenInclude(n => n.Category)
                .Include(nt => nt.NewsArticle)
                    .ThenInclude(n => n.CreatedBy)
                .Select(nt => new NewsArticleDto
                {
                    NewsArticleID = nt.NewsArticle.NewsArticleID,
                    NewsTitle = nt.NewsArticle.NewsTitle,
                    Headline = nt.NewsArticle.Headline,
                    CreatedDate = nt.NewsArticle.CreatedDate,
                    NewsContent = nt.NewsArticle.NewsContent,
                    NewsSource = nt.NewsArticle.NewsSource,
                    CategoryID = nt.NewsArticle.CategoryID,
                    CategoryName = nt.NewsArticle.Category != null ? nt.NewsArticle.Category.CategoryName : "",
                    NewsStatus = nt.NewsArticle.NewsStatus,
                    CreatedByID = nt.NewsArticle.CreatedByID,
                    CreatedByName = nt.NewsArticle.CreatedBy != null ? nt.NewsArticle.CreatedBy.AccountName : "",
                    ViewCount = nt.NewsArticle.ViewCount,
                    FeaturedImage = nt.NewsArticle.FeaturedImage
                })
                .ToListAsync();

            return Ok(new
            {
                tag = new TagDto { TagID = tag.TagID, TagName = tag.TagName, Note = tag.Note, ArticleCount = articles.Count },
                articles
            });
        }

        // POST: /api/tag
        // Enforce Business Rule: Prevent duplicate tag names!
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedName = dto.TagName.Trim();
            var exists = await _context.Tags.AnyAsync(t => t.TagName.ToLower() == normalizedName.ToLower());
            if (exists)
            {
                return BadRequest(new { message = $"Tên thẻ tag '{normalizedName}' đã tồn tại trong hệ thống. Vui lòng chọn tên khác." });
            }

            var tag = new Tag
            {
                TagName = normalizedName,
                Note = dto.Note
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "CREATE",
                "Tag",
                tag.TagID.ToString(),
                null,
                new { tag.TagName, tag.Note }
            );

            return CreatedAtAction(nameof(GetById), new { id = tag.TagID }, new { id = tag.TagID, message = "Tạo thẻ tag thành công!" });
        }

        // PUT: /api/tag/{id}
        // Enforce Business Rule: Prevent duplicate tag names!
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTagDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
                return NotFound(new { message = $"Không tìm thấy thẻ tag #{id}." });

            var normalizedName = dto.TagName.Trim();
            var duplicate = await _context.Tags.AnyAsync(t => t.TagID != id && t.TagName.ToLower() == normalizedName.ToLower());
            if (duplicate)
            {
                return BadRequest(new { message = $"Tên thẻ tag '{normalizedName}' đã được sử dụng bởi một thẻ khác." });
            }

            var beforeData = new { tag.TagName, tag.Note };

            tag.TagName = normalizedName;
            tag.Note = dto.Note;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "UPDATE",
                "Tag",
                id.ToString(),
                beforeData,
                new { tag.TagName, tag.Note }
            );

            return Ok(new { message = "Cập nhật thẻ tag thành công!", id = tag.TagID });
        }

        // DELETE: /api/tag/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _context.Tags.Include(t => t.NewsTags).FirstOrDefaultAsync(t => t.TagID == id);
            if (tag == null)
                return NotFound(new { message = $"Không tìm thấy thẻ tag #{id}." });

            var beforeData = new { tag.TagName, tag.Note, ArticleCount = tag.NewsTags.Count };

            _context.NewsTags.RemoveRange(tag.NewsTags);
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "DELETE",
                "Tag",
                id.ToString(),
                beforeData,
                null
            );

            return Ok(new { message = $"Đã xóa thành công thẻ tag '{tag.TagName}'." });
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        private string? GetCurrentUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    }
}
