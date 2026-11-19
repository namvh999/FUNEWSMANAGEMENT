using System.Security.Claims;
using FUNewsManagement_CoreAPI.Data;
using FUNewsManagement_CoreAPI.DTOs;
using FUNewsManagement_CoreAPI.Hubs;
using FUNewsManagement_CoreAPI.Models;
using FUNewsManagement_CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsArticleController : ControllerBase
    {
        private readonly FUNewsDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IWebHostEnvironment _env;

        public NewsArticleController(
            FUNewsDbContext context,
            IAuditService auditService,
            IHubContext<NotificationHub> hubContext,
            IWebHostEnvironment env)
        {
            _context = context;
            _auditService = auditService;
            _hubContext = hubContext;
            _env = env;
        }

        // GET: /api/news
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? status,
            [FromQuery] string? sortBy = "date_desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .Include(n => n.NewsTags)
                    .ThenInclude(nt => nt.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(n => n.NewsTitle.ToLower().Contains(term) ||
                                         (n.Headline != null && n.Headline.ToLower().Contains(term)) ||
                                         n.NewsContent.ToLower().Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(n => n.CategoryID == categoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(n => n.NewsStatus == status.Value);
            }

            // Sorting
            query = sortBy switch
            {
                "date_asc" => query.OrderBy(n => n.CreatedDate),
                "title_asc" => query.OrderBy(n => n.NewsTitle),
                "title_desc" => query.OrderByDescending(n => n.NewsTitle),
                "views_desc" => query.OrderByDescending(n => n.ViewCount),
                _ => query.OrderByDescending(n => n.CreatedDate)
            };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsArticleDto
                {
                    NewsArticleID = n.NewsArticleID,
                    NewsTitle = n.NewsTitle,
                    Headline = n.Headline,
                    CreatedDate = n.CreatedDate,
                    NewsContent = n.NewsContent,
                    NewsSource = n.NewsSource,
                    CategoryID = n.CategoryID,
                    CategoryName = n.Category != null ? n.Category.CategoryName : "",
                    NewsStatus = n.NewsStatus,
                    CreatedByID = n.CreatedByID,
                    CreatedByName = n.CreatedBy != null ? n.CreatedBy.AccountName : "",
                    UpdatedByID = n.UpdatedByID,
                    ModifiedDate = n.ModifiedDate,
                    ViewCount = n.ViewCount,
                    FeaturedImage = n.FeaturedImage,
                    Tags = n.NewsTags.Select(nt => new TagDto
                    {
                        TagID = nt.Tag.TagID,
                        TagName = nt.Tag.TagName,
                        Note = nt.Tag.Note
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = items
            });
        }

        // GET: /api/news/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var article = await _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .Include(n => n.NewsTags)
                    .ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(n => n.NewsArticleID == id);

            if (article == null)
                return NotFound(new { message = $"Không tìm thấy bài viết có mã #{id}." });

            // Increment view count
            article.ViewCount += 1;
            await _context.SaveChangesAsync();

            var dto = new NewsArticleDto
            {
                NewsArticleID = article.NewsArticleID,
                NewsTitle = article.NewsTitle,
                Headline = article.Headline,
                CreatedDate = article.CreatedDate,
                NewsContent = article.NewsContent,
                NewsSource = article.NewsSource,
                CategoryID = article.CategoryID,
                CategoryName = article.Category != null ? article.Category.CategoryName : "",
                NewsStatus = article.NewsStatus,
                CreatedByID = article.CreatedByID,
                CreatedByName = article.CreatedBy != null ? article.CreatedBy.AccountName : "",
                UpdatedByID = article.UpdatedByID,
                ModifiedDate = article.ModifiedDate,
                ViewCount = article.ViewCount,
                FeaturedImage = article.FeaturedImage,
                Tags = article.NewsTags.Select(nt => new TagDto
                {
                    TagID = nt.Tag.TagID,
                    TagName = nt.Tag.TagName,
                    Note = nt.Tag.Note
                }).ToList()
            };

            return Ok(dto);
        }

        // POST: /api/news
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNewsArticleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _context.Categories.FindAsync(dto.CategoryID);
            if (category == null)
                return BadRequest(new { message = "Chuyên mục (CategoryID) không tồn tại." });

            var currentUserId = GetCurrentUserId() ?? 1;
            var currentUserName = GetCurrentUserName() ?? "Staff";

            var article = new NewsArticle
            {
                NewsTitle = dto.NewsTitle,
                Headline = dto.Headline,
                CreatedDate = DateTime.UtcNow,
                NewsContent = dto.NewsContent,
                NewsSource = dto.NewsSource,
                CategoryID = dto.CategoryID,
                NewsStatus = dto.NewsStatus,
                CreatedByID = currentUserId,
                ViewCount = 0,
                FeaturedImage = dto.FeaturedImage
            };

            _context.NewsArticles.Add(article);
            await _context.SaveChangesAsync();

            // Attach tags
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                foreach (var tagId in dto.TagIds.Distinct())
                {
                    if (await _context.Tags.AnyAsync(t => t.TagID == tagId))
                    {
                        _context.NewsTags.Add(new NewsTag
                        {
                            NewsArticleID = article.NewsArticleID,
                            TagID = tagId
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Audit log
            await _auditService.LogAsync(
                currentUserId.ToString(),
                currentUserName,
                "CREATE",
                "NewsArticle",
                article.NewsArticleID.ToString(),
                null,
                new { article.NewsTitle, article.CategoryID, article.NewsStatus, dto.TagIds }
            );

            // SignalR real-time notification
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    title = "Bài viết mới được tạo!",
                    message = $"Bài viết \"{article.NewsTitle}\" vừa được tạo bởi {currentUserName}.",
                    author = currentUserName,
                    articleId = article.NewsArticleID,
                    timestamp = DateTime.UtcNow
                });
            }
            catch { }

            return CreatedAtAction(nameof(GetById), new { id = article.NewsArticleID }, new { id = article.NewsArticleID, message = "Tạo bài viết thành công!" });
        }

        // PUT: /api/news/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNewsArticleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var article = await _context.NewsArticles
                .Include(n => n.NewsTags)
                .FirstOrDefaultAsync(n => n.NewsArticleID == id);

            if (article == null)
                return NotFound(new { message = $"Không tìm thấy bài viết #{id} để cập nhật." });

            var category = await _context.Categories.FindAsync(dto.CategoryID);
            if (category == null)
                return BadRequest(new { message = "Chuyên mục (CategoryID) không tồn tại." });

            var currentUserId = GetCurrentUserId() ?? 1;
            var currentUserName = GetCurrentUserName() ?? "Staff";

            var beforeData = new
            {
                article.NewsTitle,
                article.Headline,
                article.CategoryID,
                article.NewsStatus,
                article.FeaturedImage
            };

            article.NewsTitle = dto.NewsTitle;
            article.Headline = dto.Headline;
            article.NewsContent = dto.NewsContent;
            article.NewsSource = dto.NewsSource;
            article.CategoryID = dto.CategoryID;
            article.NewsStatus = dto.NewsStatus;
            article.UpdatedByID = currentUserId;
            article.ModifiedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.FeaturedImage))
            {
                article.FeaturedImage = dto.FeaturedImage;
            }

            // Update tags: remove old tags and add new ones
            _context.NewsTags.RemoveRange(article.NewsTags);
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                foreach (var tagId in dto.TagIds.Distinct())
                {
                    if (await _context.Tags.AnyAsync(t => t.TagID == tagId))
                    {
                        _context.NewsTags.Add(new NewsTag
                        {
                            NewsArticleID = article.NewsArticleID,
                            TagID = tagId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                currentUserId.ToString(),
                currentUserName,
                "UPDATE",
                "NewsArticle",
                article.NewsArticleID.ToString(),
                beforeData,
                new { dto.NewsTitle, dto.CategoryID, dto.NewsStatus, dto.TagIds }
            );

            return Ok(new { message = "Cập nhật bài viết thành công!", id = article.NewsArticleID });
        }

        // DELETE: /api/news/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.NewsArticles
                .Include(n => n.NewsTags)
                .FirstOrDefaultAsync(n => n.NewsArticleID == id);

            if (article == null)
                return NotFound(new { message = $"Không tìm thấy bài viết #{id} để xóa." });

            var currentUserId = GetCurrentUserId() ?? 1;
            var currentUserName = GetCurrentUserName() ?? "Staff";

            var beforeData = new { article.NewsTitle, article.CategoryID, article.CreatedDate };

            _context.NewsTags.RemoveRange(article.NewsTags);
            _context.NewsArticles.Remove(article);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                currentUserId.ToString(),
                currentUserName,
                "DELETE",
                "NewsArticle",
                id.ToString(),
                beforeData,
                null
            );

            return Ok(new { message = $"Đã xóa thành công bài viết #{id}." });
        }

        // POST: /api/news/upload-image
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn một file ảnh hợp lệ." });

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Định dạng file không được hỗ trợ. Chỉ chấp nhận các định dạng .jpg, .jpeg, .png, .webp, .gif." });
            }

            // Validate file size (Max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { message = "Kích thước file vượt quá giới hạn cho phép (Tối đa 5MB)." });
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/{uniqueFileName}";
            return Ok(new { url = relativeUrl, fileName = uniqueFileName, message = "Tải ảnh lên thành công!" });
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int id)) return id;
            return null;
        }

        private string? GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
