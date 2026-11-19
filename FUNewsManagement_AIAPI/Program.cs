using FUNewsManagement_AIAPI.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiTagService, AiTagService>();

// 2. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FUNewsManagement AI API",
        Version = "v1",
        Description = "AI-powered Tag Suggestion & Learning Cache API (PRN232 Assignment 02)"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FUNews AI API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Service = "FUNewsManagement AI API",
    Status = "Healthy",
    Version = "2.0.0",
    Swagger = "/swagger"
}));

app.Run();
