using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FUNewsManagement_AnalyticsAPI.Models
{
    [Table("Category")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
    }

    [Table("SystemAccount")]
    public class SystemAccount
    {
        [Key]
        public int AccountID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public int AccountRole { get; set; }
    }

    [Table("Tag")]
    public class Tag
    {
        [Key]
        public int TagID { get; set; }
        public string TagName { get; set; } = string.Empty;
        public virtual ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
    }

    [Table("NewsArticle")]
    public class NewsArticle
    {
        [Key]
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string? Headline { get; set; }
        public DateTime CreatedDate { get; set; }
        public string NewsContent { get; set; } = string.Empty;
        public string? NewsSource { get; set; }
        public int CategoryID { get; set; }
        public virtual Category? Category { get; set; }
        public bool NewsStatus { get; set; }
        public int CreatedByID { get; set; }
        public virtual SystemAccount? CreatedBy { get; set; }
        public int ViewCount { get; set; }
        public string? FeaturedImage { get; set; }
        public virtual ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
    }

    [Table("NewsTag")]
    public class NewsTag
    {
        public int NewsArticleID { get; set; }
        public virtual NewsArticle NewsArticle { get; set; } = null!;
        public int TagID { get; set; }
        public virtual Tag Tag { get; set; } = null!;
    }

    // Response Models
    public class DashboardStatsDto
    {
        public int TotalArticles { get; set; }
        public int ActiveArticles { get; set; }
        public int InactiveArticles { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }
        public int TotalViews { get; set; }
        public List<CategoryCountDto> ArticlesByCategory { get; set; } = new();
        public List<StatusCountDto> ArticlesByStatus { get; set; } = new();
        public List<AuthorStatsDto> ArticlesByAuthor { get; set; } = new();
        public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    }

    public class CategoryCountDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ArticleCount { get; set; }
    }

    public class StatusCountDto
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AuthorStatsDto
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int ArticleCount { get; set; }
        public int TotalViews { get; set; }
    }

    public class MonthlyTrendDto
    {
        public string MonthYear { get; set; } = string.Empty;
        public int ArticleCount { get; set; }
    }

    public class RecommendedArticleDto
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string? Headline { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string? FeaturedImage { get; set; }
        public DateTime CreatedDate { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }
}
