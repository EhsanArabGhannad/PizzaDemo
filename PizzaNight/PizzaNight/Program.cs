using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Configuration;
using PizzaNight.Data;
using PizzaNight.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PizzaNightDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PizzaNight")));
builder.Services.AddScoped<OrderSubmissionService>();
builder.Services.AddScoped<ShopOperationsService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AdminCredentialValidator>();
builder.Services.AddOptions<AdminOptions>()
    .Bind(builder.Configuration.GetSection(AdminOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "Admin username is required.")
    .Validate(options => options.Password.Length >= 12, "Admin password must be at least 12 characters.")
    .ValidateOnStart();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/access-denied";
        options.Cookie.Name = "PizzaKnight.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("orders", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

if (builder.Environment.IsDevelopment())
{
    var developmentKeysPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data",
        "DataProtection-Keys");

    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(developmentKeysPath))
        .SetApplicationName("PizzaNight");
}

var app = builder.Build();

var appDataPath = Path.Combine(app.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PizzaNightDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
