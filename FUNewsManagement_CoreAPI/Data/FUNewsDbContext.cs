using FUNewsManagement_CoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagement_CoreAPI.Data
{
    public class FUNewsDbContext : DbContext
    {
        public FUNewsDbContext(DbContextOptions<FUNewsDbContext> options) : base(options)
        {
        }

        public DbSet<SystemAccount> SystemAccounts { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<NewsArticle> NewsArticles { get; set; } = null!;
        public DbSet<NewsTag> NewsTags { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<LearningTag> LearningTags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite Key for NewsTag
            modelBuilder.Entity<NewsTag>()
                .HasKey(nt => new { nt.NewsArticleID, nt.TagID });

            modelBuilder.Entity<NewsTag>()
                .HasOne(nt => nt.NewsArticle)
                .WithMany(n => n.NewsTags)
                .HasForeignKey(nt => nt.NewsArticleID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NewsTag>()
                .HasOne(nt => nt.Tag)
                .WithMany(t => t.NewsTags)
                .HasForeignKey(nt => nt.TagID)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing category
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany()
                .HasForeignKey(c => c.ParentCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign keys on NewsArticle
            modelBuilder.Entity<NewsArticle>()
                .HasOne(n => n.Category)
                .WithMany(c => c.NewsArticles)
                .HasForeignKey(n => n.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NewsArticle>()
                .HasOne(n => n.CreatedBy)
                .WithMany(a => a.CreatedArticles)
                .HasForeignKey(n => n.CreatedByID)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index for TagName
            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.TagName)
                .IsUnique();

            // Unique index for AccountEmail
            modelBuilder.Entity<SystemAccount>()
                .HasIndex(a => a.AccountEmail)
                .IsUnique();
        }

        public void SeedInitialData()
        {
            if (!SystemAccounts.Any())
            {
                SystemAccounts.AddRange(
                    new SystemAccount { AccountID = 1, AccountName = "System Administrator", AccountEmail = "admin@funews.com", AccountRole = 1, AccountPassword = "admin@123" },
                    new SystemAccount { AccountID = 2, AccountName = "Staff Editor 01", AccountEmail = "staff1@funews.com", AccountRole = 2, AccountPassword = "staff@123" },
                    new SystemAccount { AccountID = 3, AccountName = "Staff Writer 02", AccountEmail = "staff2@funews.com", AccountRole = 2, AccountPassword = "staff@123" }
                );
                SaveChanges();
            }

            if (!Categories.Any())
            {
                Categories.AddRange(
                    new Category { CategoryID = 1, CategoryName = "Technology", CategoryDescription = "Latest news on tech gadgets, AI, software, and IT trends.", IsActive = true },
                    new Category { CategoryID = 2, CategoryName = "Business & Economy", CategoryDescription = "Global markets, finance, startups, and company reports.", IsActive = true },
                    new Category { CategoryID = 3, CategoryName = "Science & Space", CategoryDescription = "Scientific breakthroughs, astronomy, biology, and research.", IsActive = true },
                    new Category { CategoryID = 4, CategoryName = "Education & Career", CategoryDescription = "Higher education, scholarship, and skill development.", IsActive = true },
                    new Category { CategoryID = 5, CategoryName = "Entertainment", CategoryDescription = "Movies, music, gaming, and pop culture.", IsActive = true },
                    new Category { CategoryID = 6, CategoryName = "Health & Wellness", CategoryDescription = "Medical research, lifestyle, and nutrition.", IsActive = true }
                );
                SaveChanges();
            }

            if (!Tags.Any())
            {
                Tags.AddRange(
                    new Tag { TagID = 1, TagName = "AI", Note = "Artificial Intelligence & Machine Learning" },
                    new Tag { TagID = 2, TagName = "DotNet8", Note = "Microsoft .NET 8, C#, EF Core ecosystem" },
                    new Tag { TagID = 3, TagName = "Cloud", Note = "Cloud computing, Azure, AWS, and GCP" },
                    new Tag { TagID = 4, TagName = "Microservices", Note = "Distributed systems and API architectures" },
                    new Tag { TagID = 5, TagName = "CyberSecurity", Note = "Information security and privacy updates" },
                    new Tag { TagID = 6, TagName = "Startups", Note = "Venture capital and emerging business models" },
                    new Tag { TagID = 7, TagName = "Finance", Note = "Fintech, stocks, banking, and crypto" },
                    new Tag { TagID = 8, TagName = "Innovation", Note = "Tech and scientific breakthroughs" }
                );
                SaveChanges();
            }

            if (!NewsArticles.Any())
            {
                var a1 = new NewsArticle
                {
                    NewsArticleID = 1,
                    NewsTitle = "Microsoft Releases .NET 8 with Massive Performance Boost",
                    Headline = ".NET 8 is LTS release bringing native AOT, C# 12, and ASP.NET Core enhancements.",
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    NewsContent = "Microsoft has officially announced the launch of .NET 8, delivering substantial performance improvements across runtime, ASP.NET Core, and Entity Framework Core. The new release introduces full Native AOT support, enhanced cloud-native capabilities, and extensive C# 12 features.",
                    NewsSource = "Microsoft Developer Blog",
                    CategoryID = 1,
                    NewsStatus = true,
                    CreatedByID = 2,
                    ViewCount = 450,
                    FeaturedImage = "/uploads/dotnet8_announcement.png"
                };

                var a2 = new NewsArticle
                {
                    NewsArticleID = 2,
                    NewsTitle = "Generative AI Transforming the Software Development Landscape",
                    Headline = "How LLMs and AI Copilots are revolutionizing developer workflows.",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    NewsContent = "Artificial Intelligence continues to reshape software engineering with intelligent code completions, automated test generations, and context-aware debugging. Engineering leaders report up to 40% productivity boosts for repetitive coding tasks.",
                    NewsSource = "TechCrunch AI News",
                    CategoryID = 1,
                    NewsStatus = true,
                    CreatedByID = 2,
                    ViewCount = 820,
                    FeaturedImage = "/uploads/ai_revolution.png"
                };

                var a3 = new NewsArticle
                {
                    NewsArticleID = 3,
                    NewsTitle = "Global Fintech Investments Rebound in Q4 2026",
                    Headline = "Digital payments and AI-driven banking lead financial technology resurgence.",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    NewsContent = "Global venture capital funding into financial technology firms has picked up momentum after a cautious period. Investors are heavily funding AI-driven credit scoring, cross-border payments, and fraud prevention startups.",
                    NewsSource = "Financial Times",
                    CategoryID = 2,
                    NewsStatus = true,
                    CreatedByID = 3,
                    ViewCount = 310,
                    FeaturedImage = "/uploads/fintech_growth.png"
                };

                var a4 = new NewsArticle
                {
                    NewsArticleID = 4,
                    NewsTitle = "SpaceX and NASA Announce Next Generation Deep Space Exploration Missions",
                    Headline = "New collaborative roadmap for lunar base construction and orbital science.",
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    NewsContent = "NASA together with commercial space partners including SpaceX has presented the updated roadmap for prolonged human presence on the Moon and preparations for Mars exploration.",
                    NewsSource = "Space News Daily",
                    CategoryID = 3,
                    NewsStatus = true,
                    CreatedByID = 2,
                    ViewCount = 610,
                    FeaturedImage = "/uploads/space_exploration.png"
                };

                NewsArticles.AddRange(a1, a2, a3, a4);
                SaveChanges();

                NewsTags.AddRange(
                    new NewsTag { NewsArticleID = 1, TagID = 1 },
                    new NewsTag { NewsArticleID = 1, TagID = 2 },
                    new NewsTag { NewsArticleID = 1, TagID = 4 },
                    new NewsTag { NewsArticleID = 2, TagID = 1 },
                    new NewsTag { NewsArticleID = 2, TagID = 3 },
                    new NewsTag { NewsArticleID = 2, TagID = 8 },
                    new NewsTag { NewsArticleID = 3, TagID = 6 },
                    new NewsTag { NewsArticleID = 3, TagID = 7 },
                    new NewsTag { NewsArticleID = 4, TagID = 8 }
                );
                SaveChanges();
            }

            if (!LearningTags.Any())
            {
                LearningTags.AddRange(
                    new LearningTag { TagName = "AI", Frequency = 25, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "DotNet8", Frequency = 18, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "Cloud", Frequency = 15, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "Microservices", Frequency = 12, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "CyberSecurity", Frequency = 9, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "Startups", Frequency = 7, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "Finance", Frequency = 6, LastUsed = DateTime.UtcNow },
                    new LearningTag { TagName = "Innovation", Frequency = 10, LastUsed = DateTime.UtcNow }
                );
                SaveChanges();
            }

            if (!AuditLogs.Any())
            {
                AuditLogs.AddRange(
                    new AuditLog { UserId = "2", UserName = "staff1@funews.com", Action = "CREATE", Entity = "NewsArticle", EntityId = "1", AfterData = "{\"Title\":\"Microsoft Releases .NET 8\"}", Timestamp = DateTime.UtcNow.AddDays(-5) },
                    new AuditLog { UserId = "2", UserName = "staff1@funews.com", Action = "CREATE", Entity = "NewsArticle", EntityId = "2", AfterData = "{\"Title\":\"Generative AI Transforming Software\"}", Timestamp = DateTime.UtcNow.AddDays(-3) }
                );
                SaveChanges();
            }
        }
    }
}
