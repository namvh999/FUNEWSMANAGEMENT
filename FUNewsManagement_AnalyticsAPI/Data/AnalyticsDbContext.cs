using FUNewsManagement_AnalyticsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_AnalyticsAPI.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<SystemAccount> SystemAccounts { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<NewsArticle> NewsArticles { get; set; } = null!;
        public DbSet<NewsTag> NewsTags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NewsTag>()
                .HasKey(nt => new { nt.NewsArticleID, nt.TagID });

            modelBuilder.Entity<NewsTag>()
                .HasOne(nt => nt.NewsArticle)
                .WithMany(n => n.NewsTags)
                .HasForeignKey(nt => nt.NewsArticleID);

            modelBuilder.Entity<NewsTag>()
                .HasOne(nt => nt.Tag)
                .WithMany(t => t.NewsTags)
                .HasForeignKey(nt => nt.TagID);
        }

        public void SeedSampleDataIfEmpty()
        {
            if (!Categories.Any())
            {
                Categories.AddRange(
                    new Category { CategoryID = 1, CategoryName = "Technology", IsActive = true },
                    new Category { CategoryID = 2, CategoryName = "Business & Economy", IsActive = true },
                    new Category { CategoryID = 3, CategoryName = "Science & Space", IsActive = true },
                    new Category { CategoryID = 4, CategoryName = "Education & Career", IsActive = true }
                );
                SaveChanges();
            }

            if (!SystemAccounts.Any())
            {
                SystemAccounts.AddRange(
                    new SystemAccount { AccountID = 1, AccountName = "Admin User", AccountEmail = "admin@funews.com", AccountRole = 1 },
                    new SystemAccount { AccountID = 2, AccountName = "Staff Editor 01", AccountEmail = "staff1@funews.com", AccountRole = 2 },
                    new SystemAccount { AccountID = 3, AccountName = "Staff Writer 02", AccountEmail = "staff2@funews.com", AccountRole = 2 }
                );
                SaveChanges();
            }

            if (!Tags.Any())
            {
                Tags.AddRange(
                    new Tag { TagID = 1, TagName = "AI" },
                    new Tag { TagID = 2, TagName = "DotNet8" },
                    new Tag { TagID = 3, TagName = "Cloud" }
                );
                SaveChanges();
            }

            if (!NewsArticles.Any())
            {
                NewsArticles.AddRange(
                    new NewsArticle
                    {
                        NewsArticleID = 1,
                        NewsTitle = "Microsoft Releases .NET 8",
                        Headline = ".NET 8 brings native AOT and speed",
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        NewsContent = "Content of article 1",
                        CategoryID = 1,
                        NewsStatus = true,
                        CreatedByID = 2,
                        ViewCount = 450
                    },
                    new NewsArticle
                    {
                        NewsArticleID = 2,
                        NewsTitle = "Generative AI in Software",
                        Headline = "AI Copilots transform software",
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        NewsContent = "Content of article 2",
                        CategoryID = 1,
                        NewsStatus = true,
                        CreatedByID = 2,
                        ViewCount = 820
                    },
                    new NewsArticle
                    {
                        NewsArticleID = 3,
                        NewsTitle = "Fintech Investments Rebound",
                        Headline = "Digital payments lead growth",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        NewsContent = "Content of article 3",
                        CategoryID = 2,
                        NewsStatus = true,
                        CreatedByID = 3,
                        ViewCount = 310
                    }
                );
                SaveChanges();

                NewsTags.AddRange(
                    new NewsTag { NewsArticleID = 1, TagID = 1 },
                    new NewsTag { NewsArticleID = 1, TagID = 2 },
                    new NewsTag { NewsArticleID = 2, TagID = 1 }
                );
                SaveChanges();
            }
        }
    }
}
