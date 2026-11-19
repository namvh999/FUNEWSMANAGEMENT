using System.Text;
using FUNewsManagement_AnalyticsAPI.Data;
using FUNewsManagement_AnalyticsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_AnalyticsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsDbContext _context;

        public AnalyticsController(AnalyticsDbContext context)
        {
            _context = context;
        }

        // GET: /api/analytics/dashboard
        // Display total articles by category and status with advanced filtering
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? categoryId,
            [FromQuery] int? authorId,
            [FromQuery] bool? status)
        {
            var query = _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(n => n.CreatedDate <= end);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(n => n.CategoryID == categoryId.Value);

            if (authorId.HasValue && authorId.Value > 0)
                query = query.Where(n => n.CreatedByID == authorId.Value);

            if (status.HasValue)
                query = query.Where(n => n.NewsStatus == status.Value);

            var totalArticles = await query.CountAsync();
            var activeArticles = await query.CountAsync(n => n.NewsStatus);
            var inactiveArticles = totalArticles - activeArticles;
            var totalViews = await query.SumAsync(n => (int?)n.ViewCount) ?? 0;

            // Articles by Category
            var byCategory = await query
                .GroupBy(n => new { n.CategoryID, n.Category!.CategoryName })
                .Select(g => new CategoryCountDto
                {
                    CategoryId = g.Key.CategoryID,
                    CategoryName = g.Key.CategoryName,
                    ArticleCount = g.Count()
                })
                .OrderByDescending(c => c.ArticleCount)
                .ToListAsync();

            // Articles by Status
            var byStatus = new List<StatusCountDto>
            {
                new StatusCountDto { StatusName = "Hoạt động (Active)", Count = activeArticles },
                new StatusCountDto { StatusName = "Ngừng hoạt động (Inactive)", Count = inactiveArticles }
            };

            // Articles by Author
            var byAuthor = await query
                .GroupBy(n => new { n.CreatedByID, n.CreatedBy!.AccountName })
                .Select(g => new AuthorStatsDto
                {
                    AuthorId = g.Key.CreatedByID,
                    AuthorName = g.Key.AccountName,
                    ArticleCount = g.Count(),
                    TotalViews = g.Sum(n => n.ViewCount)
                })
                .OrderByDescending(a => a.ArticleCount)
                .ToListAsync();

            // Total Categories & Tags
            var totalCategories = await _context.Categories.CountAsync();
            var totalTags = await _context.Tags.CountAsync();

            var dto = new DashboardStatsDto
            {
                TotalArticles = totalArticles,
                ActiveArticles = activeArticles,
                InactiveArticles = inactiveArticles,
                TotalCategories = totalCategories,
                TotalTags = totalTags,
                TotalViews = totalViews,
                ArticlesByCategory = byCategory,
                ArticlesByStatus = byStatus,
                ArticlesByAuthor = byAuthor
            };

            return Ok(dto);
        }

        // GET: /api/analytics/trending
        // List trending (most viewed) articles
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending(
            [FromQuery] int limit = 5,
            [FromQuery] int? categoryId = null)
        {
            var query = _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .Where(n => n.NewsStatus)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(n => n.CategoryID == categoryId.Value);
            }

            var trending = await query
                .OrderByDescending(n => n.ViewCount)
                .ThenByDescending(n => n.CreatedDate)
                .Take(limit)
                .Select(n => new
                {
                    n.NewsArticleID,
                    n.NewsTitle,
                    n.Headline,
                    CategoryName = n.Category != null ? n.Category.CategoryName : "",
                    AuthorName = n.CreatedBy != null ? n.CreatedBy.AccountName : "",
                    n.ViewCount,
                    n.CreatedDate,
                    n.FeaturedImage
                })
                .ToListAsync();

            return Ok(trending);
        }

        // GET: /api/analytics/export
        // Export Excel / CSV Report
        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? categoryId)
        {
            var query = _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(n => n.CreatedDate <= end);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(n => n.CategoryID == categoryId.Value);

            var articles = await query
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            // Build CSV/Excel spreadsheet content with UTF-8 BOM so Excel opens Vietnamese characters cleanly
            var sb = new StringBuilder();
            // Header
            sb.AppendLine("ID,Tiêu đề bài viết,Chuyên mục,Tác giả,Ngày tạo,Lượt xem,Trạng thái,Nguồn");

            foreach (var a in articles)
            {
                var title = EscapeCsv(a.NewsTitle);
                var category = EscapeCsv(a.Category?.CategoryName ?? "");
                var author = EscapeCsv(a.CreatedBy?.AccountName ?? "");
                var date = a.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                var views = a.ViewCount.ToString();
                var status = a.NewsStatus ? "Active" : "Inactive";
                var source = EscapeCsv(a.NewsSource ?? "");

                sb.AppendLine($"{a.NewsArticleID},{title},{category},{author},{date},{views},{status},{source}");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var finalBytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, finalBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, finalBytes, preamble.Length, contentBytes.Length);

            var fileName = $"FUNews_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(finalBytes, "text/csv; charset=utf-8", fileName);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
