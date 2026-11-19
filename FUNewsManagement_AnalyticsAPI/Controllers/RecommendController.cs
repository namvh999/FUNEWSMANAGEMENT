using FUNewsManagement_AnalyticsAPI.Data;
using FUNewsManagement_AnalyticsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_AnalyticsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendController : ControllerBase
    {
        private readonly AnalyticsDbContext _context;

        public RecommendController(AnalyticsDbContext context)
        {
            _context = context;
        }

        // GET: /api/recommend/{id}
        // Suggest related articles based on category or tags (up to 3 articles)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecommendations(int id, [FromQuery] int limit = 3)
        {
            var targetArticle = await _context.NewsArticles
                .Include(n => n.NewsTags)
                .FirstOrDefaultAsync(n => n.NewsArticleID == id);

            if (targetArticle == null)
            {
                return NotFound(new { message = $"Không tìm thấy bài viết #{id}." });
            }

            var targetTagIds = targetArticle.NewsTags.Select(nt => nt.TagID).ToList();
            var targetCategoryId = targetArticle.CategoryID;

            // 1. Articles with shared tags (highest priority)
            var articlesWithTags = await _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.NewsTags)
                .Where(n => n.NewsArticleID != id && n.NewsStatus && n.NewsTags.Any(nt => targetTagIds.Contains(nt.TagID)))
                .OrderByDescending(n => n.NewsTags.Count(nt => targetTagIds.Contains(nt.TagID)))
                .ThenByDescending(n => n.ViewCount)
                .Take(limit)
                .Select(n => new RecommendedArticleDto
                {
                    NewsArticleID = n.NewsArticleID,
                    NewsTitle = n.NewsTitle,
                    Headline = n.Headline,
                    CategoryName = n.Category != null ? n.Category.CategoryName : "",
                    ViewCount = n.ViewCount,
                    FeaturedImage = n.FeaturedImage,
                    CreatedDate = n.CreatedDate,
                    MatchReason = "Chung thẻ Tags"
                })
                .ToListAsync();

            var result = new List<RecommendedArticleDto>(articlesWithTags);

            // 2. If fewer than limit, fill with articles from the same category
            if (result.Count < limit)
            {
                var existingIds = result.Select(r => r.NewsArticleID).Concat(new[] { id }).ToList();
                var remainingCount = limit - result.Count;

                var sameCategoryArticles = await _context.NewsArticles
                    .Include(n => n.Category)
                    .Where(n => !existingIds.Contains(n.NewsArticleID) && n.NewsStatus && n.CategoryID == targetCategoryId)
                    .OrderByDescending(n => n.ViewCount)
                    .ThenByDescending(n => n.CreatedDate)
                    .Take(remainingCount)
                    .Select(n => new RecommendedArticleDto
                    {
                        NewsArticleID = n.NewsArticleID,
                        NewsTitle = n.NewsTitle,
                        Headline = n.Headline,
                        CategoryName = n.Category != null ? n.Category.CategoryName : "",
                        ViewCount = n.ViewCount,
                        FeaturedImage = n.FeaturedImage,
                        CreatedDate = n.CreatedDate,
                        MatchReason = "Cùng chuyên mục"
                    })
                    .ToListAsync();

                result.AddRange(sameCategoryArticles);
            }

            // 3. If still fewer than limit, fill with general trending articles
            if (result.Count < limit)
            {
                var existingIds = result.Select(r => r.NewsArticleID).Concat(new[] { id }).ToList();
                var remainingCount = limit - result.Count;

                var fallbackArticles = await _context.NewsArticles
                    .Include(n => n.Category)
                    .Where(n => !existingIds.Contains(n.NewsArticleID) && n.NewsStatus)
                    .OrderByDescending(n => n.ViewCount)
                    .Take(remainingCount)
                    .Select(n => new RecommendedArticleDto
                    {
                        NewsArticleID = n.NewsArticleID,
                        NewsTitle = n.NewsTitle,
                        Headline = n.Headline,
                        CategoryName = n.Category != null ? n.Category.CategoryName : "",
                        ViewCount = n.ViewCount,
                        FeaturedImage = n.FeaturedImage,
                        CreatedDate = n.CreatedDate,
                        MatchReason = "Bài viết nổi bật"
                    })
                    .ToListAsync();

                result.AddRange(fallbackArticles);
            }

            return Ok(result);
        }
    }
}
