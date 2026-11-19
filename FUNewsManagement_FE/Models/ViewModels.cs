namespace FUNewsManagement_FE.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserSessionDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    public class NewsArticleViewModel
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string? Headline { get; set; }
        public DateTime CreatedDate { get; set; }
        public string NewsContent { get; set; } = string.Empty;
        public string? NewsSource { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool NewsStatus { get; set; }
        public int CreatedByID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string? FeaturedImage { get; set; }
        public List<TagViewModel> Tags { get; set; } = new();
    }

    public class PagedNewsResult
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<NewsArticleViewModel> Data { get; set; } = new();
    }

    public class CategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryDescription { get; set; }
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public bool IsActive { get; set; }
        public int ArticleCount { get; set; }
    }

    public class TagViewModel
    {
        public int TagID { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int ArticleCount { get; set; }
    }

    public class AccountViewModel
    {
        public int AccountID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public int AccountRole { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int CreatedArticleCount { get; set; }
    }

    public class AuditLogViewModel
    {
        public int AuditID { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? BeforeData { get; set; }
        public string? AfterData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PagedAuditLogResult
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<AuditLogViewModel> Data { get; set; } = new();
    }

    public class AiTagDto
    {
        public string TagName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class DashboardViewModel
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

    public class RecommendedArticleViewModel
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
