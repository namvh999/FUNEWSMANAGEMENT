using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FUNewsManagement_CoreAPI.Models
{
    [Table("SystemAccount")]
    public class SystemAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountID { get; set; }

        [Required, MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string AccountEmail { get; set; } = string.Empty;

        public int AccountRole { get; set; } // 1: Admin, 2: Staff

        [Required, MaxLength(255)]
        public string AccountPassword { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        public virtual ICollection<NewsArticle> CreatedArticles { get; set; } = new List<NewsArticle>();
    }

    [Table("Category")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? CategoryDescription { get; set; }

        public int? ParentCategoryID { get; set; }

        [ForeignKey("ParentCategoryID")]
        public virtual Category? ParentCategory { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
    }

    [Table("Tag")]
    public class Tag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TagID { get; set; }

        [Required, MaxLength(50)]
        public string TagName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Note { get; set; }

        public virtual ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
    }

    [Table("NewsArticle")]
    public class NewsArticle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NewsArticleID { get; set; }

        [Required, MaxLength(400)]
        public string NewsTitle { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Headline { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string NewsContent { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? NewsSource { get; set; }

        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }

        public bool NewsStatus { get; set; } = true; // 1: Active, 0: Inactive

        public int CreatedByID { get; set; }

        [ForeignKey("CreatedByID")]
        public virtual SystemAccount? CreatedBy { get; set; }

        public int? UpdatedByID { get; set; }

        [ForeignKey("UpdatedByID")]
        public virtual SystemAccount? UpdatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int ViewCount { get; set; } = 0;

        [MaxLength(500)]
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

    [Table("AuditLog")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditID { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN

        [Required, MaxLength(100)]
        public string Entity { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        public string? BeforeData { get; set; }

        public string? AfterData { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [Table("LearningTag")]
    public class LearningTag
    {
        [Key]
        [MaxLength(100)]
        public string TagName { get; set; } = string.Empty;

        public int Frequency { get; set; } = 1;

        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }
}
