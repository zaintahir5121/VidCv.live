using aibabag.Data;
using aibabag.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=aibabag.db"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google";
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? string.Empty;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? string.Empty;
    options.SaveTokens = true;
    options.CallbackPath = "/signin-google";
    options.Scope.Add("https://www.googleapis.com/auth/user.birthday.read");
});

builder.Services.AddScoped<IAstrologyService, AstrologyService>();
builder.Services.AddScoped<IDetailedAstrologyService, DetailedAstrologyService>();
builder.Services.AddScoped<ISocialPublisherService, SocialPublisherService>();
builder.Services.AddHttpClient<IFamousBirthdayService, WikimediaFamousBirthdayService>();
builder.Services.Configure<AiProviderOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<IAiTextService, PollinationsAiTextService>();
builder.Services.AddHttpClient<IGooglePeopleProfileService, GooglePeopleProfileService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasPendingMigrations = db.Database.GetPendingMigrations().Any();
    if (!hasPendingMigrations)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        var relationalCreator = db.Database.GetService<IRelationalDatabaseCreator>();
        var hasTables = relationalCreator.HasTables();
        if (!hasTables)
        {
            db.Database.Migrate();
        }
        else
        {
            // Existing SQLite created via EnsureCreated path: avoid migration crash on first upgraded run.
            db.Database.EnsureCreated();
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
