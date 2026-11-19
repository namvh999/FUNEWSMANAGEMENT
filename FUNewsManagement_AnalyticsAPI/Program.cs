using FUNewsManagement_AnalyticsAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 2. Database Context with fallback
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
{
    var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", false);
    if (useInMemory || string.IsNullOrEmpty(connectionString))
    {
        options.UseInMemoryDatabase("FUNewsManagementInMemory");
    }
    else
    {
        try
        {
            options.UseSqlServer(connectionString);
        }
        catch
        {
            options.UseInMemoryDatabase("FUNewsManagementInMemory");
        }
    }
});

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 4. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FUNewsManagement Analytics API",
        Version = "v1",
        Description = "Analytics, Trending, Recommendations & Reporting API (PRN232 Assignment 02)"
    });
});

var app = builder.Build();

// Seed data if in-memory
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        db.Database.EnsureCreated();
        db.SeedSampleDataIfEmpty();
    }
    catch { }
}

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FUNews Analytics API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Service = "FUNewsManagement Analytics API",
    Status = "Healthy",
    Version = "2.0.0",
    Swagger = "/swagger"
}));

app.Run();
