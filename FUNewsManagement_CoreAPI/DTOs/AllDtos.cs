using System.ComponentModel.DataAnnotations;

namespace FUNewsManagement_CoreAPI.DTOs
{
    // Auth DTOs
    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // News Article DTOs
    public class NewsArticleDto
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
        public int? UpdatedByID { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ViewCount { get; set; }
        public string? FeaturedImage { get; set; }
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
    }

    public class CreateNewsArticleDto
    {
        [Required, MaxLength(400)]
        public string NewsTitle { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Headline { get; set; }

        [Required]
        public string NewsContent { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? NewsSource { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public bool NewsStatus { get; set; } = true;

        public string? FeaturedImage { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class UpdateNewsArticleDto
    {
        [Required, MaxLength(400)]
        public string NewsTitle { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Headline { get; set; }

        [Required]
        public string NewsContent { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? NewsSource { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public bool NewsStatus { get; set; } = true;

        public string? FeaturedImage { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();
    }

    // Category DTOs
    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryDescription { get; set; }
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public bool IsActive { get; set; }
        public int ArticleCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? CategoryDescription { get; set; }

        public int? ParentCategoryID { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateCategoryDto
    {
        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? CategoryDescription { get; set; }

        public int? ParentCategoryID { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Tag DTOs
    public class TagDto
    {
        public int TagID { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int ArticleCount { get; set; }
    }

    public class CreateTagDto
    {
        [Required, MaxLength(50)]
        public string TagName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Note { get; set; }
    }

    public class UpdateTagDto
    {
        [Required, MaxLength(50)]
        public string TagName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Note { get; set; }
    }

    // Account DTOs
    public class AccountDto
    {
        public int AccountID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public int AccountRole { get; set; }
        public string RoleName => AccountRole == 1 ? "Admin" : "Staff";
        public int CreatedArticleCount { get; set; }
    }

    public class CreateAccountDto
    {
        [Required, MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string AccountEmail { get; set; } = string.Empty;

        [Required, Range(1, 2)]
        public int AccountRole { get; set; }

        [Required, MinLength(6)]
        public string AccountPassword { get; set; } = string.Empty;
    }

    public class UpdateAccountDto
    {
        [Required, MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string AccountEmail { get; set; } = string.Empty;

        [Required, Range(1, 2)]
        public int AccountRole { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Audit Log DTOs
    public class AuditLogDto
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
}
