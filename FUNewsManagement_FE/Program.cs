using FUNewsManagement_FE.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 2. Configure Session (stores JWT access_token, refresh_token, role)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".FUNews.Session";
});

// 3. Register DelegatingHandler for JWT propagation and automatic refresh
builder.Services.AddTransient<AuthTokenHandler>();

// 4. Register Offline Data & Caching Services
builder.Services.AddSingleton<OfflineDataService>();

// 5. Register Background Worker (HostedService) to refresh cache every 6 hours (Section 3.4)
builder.Services.AddHostedService<BackgroundCacheWorker>();

// 6. Define Polly Retry Policy (retries 3 times on transient errors)
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound || (int)msg.StatusCode >= 500)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, retryAttempt)));
}

// 7. Configure Named HttpClients with Polly and DelegatingHandler
var coreApiUrl = builder.Configuration["ApiUrls:CoreApi"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("CoreApi", client =>
{
    client.BaseAddress = new Uri(coreApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<AuthTokenHandler>()
.AddPolicyHandler(GetRetryPolicy());

var analyticsApiUrl = builder.Configuration["ApiUrls:AnalyticsApi"] ?? "http://localhost:5002";
builder.Services.AddHttpClient("AnalyticsApi", client =>
{
    client.BaseAddress = new Uri(analyticsApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<AuthTokenHandler>()
.AddPolicyHandler(GetRetryPolicy());

var aiApiUrl = builder.Configuration["ApiUrls:AiApi"] ?? "http://localhost:5003";
builder.Services.AddHttpClient("AiApi", client =>
{
    client.BaseAddress = new Uri(aiApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddPolicyHandler(GetRetryPolicy());

// 8. Register Typed API Client Wrappers
builder.Services.AddScoped<CoreApiClient>();
builder.Services.AddScoped<AnalyticsApiClient>();
builder.Services.AddScoped<AiApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=News}/{action=Index}/{id?}");

app.Run();
