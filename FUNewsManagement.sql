-- =========================================================================
-- DATABASE: FUNewsManagement
-- PRN232 - Assignment 02: Distributed and Intelligent News Management System
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FUNewsManagement')
BEGIN
    CREATE DATABASE FUNewsManagement;
END
GO

USE FUNewsManagement;
GO

-- 1. Drop existing tables if needed (in reverse dependency order)
IF OBJECT_ID('NewsTag', 'U') IS NOT NULL DROP TABLE NewsTag;
IF OBJECT_ID('NewsArticle', 'U') IS NOT NULL DROP TABLE NewsArticle;
IF OBJECT_ID('AuditLog', 'U') IS NOT NULL DROP TABLE AuditLog;
IF OBJECT_ID('LearningTag', 'U') IS NOT NULL DROP TABLE LearningTag;
IF OBJECT_ID('Tag', 'U') IS NOT NULL DROP TABLE Tag;
IF OBJECT_ID('Category', 'U') IS NOT NULL DROP TABLE Category;
IF OBJECT_ID('SystemAccount', 'U') IS NOT NULL DROP TABLE SystemAccount;
GO

-- 2. Create SystemAccount table
CREATE TABLE SystemAccount (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    AccountName NVARCHAR(100) NOT NULL,
    AccountEmail NVARCHAR(150) NOT NULL UNIQUE,
    AccountRole INT NOT NULL, -- 1: Admin, 2: Staff
    AccountPassword NVARCHAR(255) NOT NULL,
    RefreshToken NVARCHAR(500) NULL,
    RefreshTokenExpiryTime DATETIME NULL
);
GO

-- 3. Create Category table
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    CategoryDescription NVARCHAR(250) NULL,
    ParentCategoryID INT NULL FOREIGN KEY REFERENCES Category(CategoryID),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 4. Create Tag table
CREATE TABLE Tag (
    TagID INT IDENTITY(1,1) PRIMARY KEY,
    TagName NVARCHAR(50) NOT NULL UNIQUE,
    Note NVARCHAR(400) NULL
);
GO

-- 5. Create NewsArticle table
CREATE TABLE NewsArticle (
    NewsArticleID INT IDENTITY(1,1) PRIMARY KEY,
    NewsTitle NVARCHAR(400) NOT NULL,
    Headline NVARCHAR(250) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    NewsContent NVARCHAR(MAX) NOT NULL,
    NewsSource NVARCHAR(400) NULL,
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES Category(CategoryID),
    NewsStatus BIT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
    CreatedByID INT NOT NULL FOREIGN KEY REFERENCES SystemAccount(AccountID),
    UpdatedByID INT NULL FOREIGN KEY REFERENCES SystemAccount(AccountID),
    ModifiedDate DATETIME NULL,
    ViewCount INT NOT NULL DEFAULT 0,
    FeaturedImage NVARCHAR(500) NULL
);
GO

-- 6. Create NewsTag table (Many-to-Many junction)
CREATE TABLE NewsTag (
    NewsArticleID INT NOT NULL FOREIGN KEY REFERENCES NewsArticle(NewsArticleID) ON DELETE CASCADE,
    TagID INT NOT NULL FOREIGN KEY REFERENCES Tag(TagID) ON DELETE CASCADE,
    PRIMARY KEY (NewsArticleID, TagID)
);
GO

-- 7. Create AuditLog table
CREATE TABLE AuditLog (
    AuditID INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(100) NULL,
    UserName NVARCHAR(100) NULL,
    Action NVARCHAR(50) NOT NULL, -- CREATE, UPDATE, DELETE, LOGIN
    Entity NVARCHAR(100) NOT NULL, -- NewsArticle, Category, Tag, Account
    EntityId NVARCHAR(100) NULL,
    BeforeData NVARCHAR(MAX) NULL,
    AfterData NVARCHAR(MAX) NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- 8. Create LearningTag table (AI Tag Suggestion Memory)
CREATE TABLE LearningTag (
    TagName NVARCHAR(100) PRIMARY KEY,
    Frequency INT NOT NULL DEFAULT 1,
    LastUsed DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- =========================================================================
-- SEED DATA
-- =========================================================================

-- Seed Accounts
-- Passwords: 'admin@123' (Admin), 'staff@123' (Staff)
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
VALUES 
('System Administrator', 'admin@funews.com', 1, 'admin@123'),
('Staff Editor 01', 'staff1@funews.com', 2, 'staff@123'),
('Staff Writer 02', 'staff2@funews.com', 2, 'staff@123');
GO

-- Seed Categories
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
VALUES 
('Technology', 'Latest news on tech gadgets, AI, software, and IT trends.', NULL, 1),
('Business & Economy', 'Global markets, finance, startups, and company reports.', NULL, 1),
('Science & Space', 'Scientific breakthroughs, astronomy, biology, and research.', NULL, 1),
('Education & Career', 'Higher education, scholarship, and skill development.', NULL, 1),
('Entertainment', 'Movies, music, gaming, and pop culture.', NULL, 1),
('Health & Wellness', 'Medical research, lifestyle, and nutrition.', NULL, 1);
GO

-- Seed Tags
INSERT INTO Tag (TagName, Note)
VALUES 
('AI', 'Artificial Intelligence & Machine Learning'),
('DotNet8', 'Microsoft .NET 8, C#, EF Core ecosystem'),
('Cloud', 'Cloud computing, Azure, AWS, and GCP'),
('Microservices', 'Distributed systems and API architectures'),
('CyberSecurity', 'Information security and privacy updates'),
('Startups', 'Venture capital and emerging business models'),
('Finance', 'Fintech, stocks, banking, and crypto'),
('Innovation', 'Tech and scientific breakthroughs');
GO

-- Seed News Articles
INSERT INTO NewsArticle (NewsTitle, Headline, CreatedDate, NewsContent, NewsSource, CategoryID, NewsStatus, CreatedByID, ViewCount, FeaturedImage)
VALUES 
(
    'Microsoft Releases .NET 8 with Massive Performance Boost',
    '.NET 8 is LTS release bringing native AOT, C# 12, and ASP.NET Core enhancements.',
    DATEADD(day, -5, GETDATE()),
    'Microsoft has officially announced the launch of .NET 8, delivering substantial performance improvements across runtime, ASP.NET Core, and Entity Framework Core. The new release introduces full Native AOT support, enhanced cloud-native capabilities, and extensive C# 12 features.',
    'Microsoft Developer Blog',
    1, 1, 2, 450, '/uploads/dotnet8_announcement.png'
),
(
    'Generative AI Transforming the Software Development Landscape',
    'How LLMs and AI Copilots are revolutionizing developer workflows.',
    DATEADD(day, -3, GETDATE()),
    'Artificial Intelligence continues to reshape software engineering with intelligent code completions, automated test generations, and context-aware debugging. Engineering leaders report up to 40% productivity boosts for repetitive coding tasks.',
    'TechCrunch AI News',
    1, 1, 2, 820, '/uploads/ai_revolution.png'
),
(
    'Global Fintech Investments Rebound in Q4 2026',
    'Digital payments and AI-driven banking lead financial technology resurgence.',
    DATEADD(day, -2, GETDATE()),
    'Global venture capital funding into financial technology firms has picked up momentum after a cautious period. Investors are heavily funding AI-driven credit scoring, cross-border payments, and fraud prevention startups.',
    'Financial Times',
    2, 1, 3, 310, '/uploads/fintech_growth.png'
),
(
    'SpaceX and NASA Announce Next Generation Deep Space Exploration Missions',
    'New collaborative roadmap for lunar base construction and orbital science.',
    DATEADD(day, -1, GETDATE()),
    'NASA together with commercial space partners including SpaceX has presented the updated roadmap for prolonged human presence on the Moon and preparations for Mars exploration.',
    'Space News Daily',
    3, 1, 2, 610, '/uploads/space_exploration.png'
);
GO

-- Seed NewsTag relations
INSERT INTO NewsTag (NewsArticleID, TagID)
VALUES 
(1, 1), -- Article 1 has AI tag
(1, 2), -- Article 1 has DotNet8 tag
(1, 4), -- Article 1 has Microservices tag
(2, 1), -- Article 2 has AI tag
(2, 3), -- Article 2 has Cloud tag
(2, 8), -- Article 2 has Innovation tag
(3, 6), -- Article 3 has Startups tag
(3, 7), -- Article 3 has Finance tag
(4, 8); -- Article 4 has Innovation tag
GO

-- Seed Initial Learning Cache for AI Tag Suggestion
INSERT INTO LearningTag (TagName, Frequency, LastUsed)
VALUES 
('AI', 25, GETDATE()),
('DotNet8', 18, GETDATE()),
('Cloud', 15, GETDATE()),
('Microservices', 12, GETDATE()),
('CyberSecurity', 9, GETDATE()),
('Startups', 7, GETDATE()),
('Finance', 6, GETDATE()),
('Innovation', 10, GETDATE());
GO

-- Seed Sample Audit Logs
INSERT INTO AuditLog (UserId, UserName, Action, Entity, EntityId, BeforeData, AfterData, Timestamp)
VALUES 
('2', 'staff1@funews.com', 'CREATE', 'NewsArticle', '1', NULL, '{"Title":"Microsoft Releases .NET 8 with Massive Performance Boost","CategoryID":1}', DATEADD(day, -5, GETDATE())),
('2', 'staff1@funews.com', 'CREATE', 'NewsArticle', '2', NULL, '{"Title":"Generative AI Transforming the Software Development Landscape","CategoryID":1}', DATEADD(day, -3, GETDATE())),
('3', 'staff2@funews.com', 'CREATE', 'NewsArticle', '3', NULL, '{"Title":"Global Fintech Investments Rebound in Q4 2026","CategoryID":2}', DATEADD(day, -2, GETDATE())),
('1', 'admin@funews.com', 'UPDATE', 'Category', '1', '{"CategoryName":"Tech"}', '{"CategoryName":"Technology"}', DATEADD(day, -1, GETDATE()));
GO

PRINT 'FUNewsManagement database created and seeded successfully!';
GO
