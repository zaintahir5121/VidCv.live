using AGONECompliance.Client.Pages;
using AGONECompliance.Components;
using AGONECompliance.Data;
using AGONECompliance.Jobs;
using AGONECompliance.Options;
using AGONECompliance.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureOptions>(builder.Configuration.GetSection(AzureOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var sqlConnection = builder.Configuration.GetConnectionString("SqlServer");
if (!string.IsNullOrWhiteSpace(sqlConnection))
{
    builder.Services.AddDbContext<ComplianceDbContext>(options =>
        options.UseSqlServer(sqlConnection, sql =>
        {
            sql.EnableRetryOnFailure(5);
            sql.CommandTimeout(60);
        }));
}
else
{
    var sqlitePath = builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=agonecompliance.db";
    builder.Services.AddDbContext<ComplianceDbContext>(options => options.UseSqlite(sqlitePath));
}

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
builder.Services.AddScoped<IComplianceAiService, ComplianceAiService>();
builder.Services.AddScoped<IComplianceSearchService, ComplianceSearchService>();
builder.Services.AddScoped<IEvaluationOrchestrator, EvaluationOrchestrator>();

builder.Services.AddQuartz(options =>
{
    var jobKey = new JobKey(nameof(EvaluationWorkerJob));
    options.AddJob<EvaluationWorkerJob>(config => config.WithIdentity(jobKey));
    options.AddTrigger(trigger => trigger
        .ForJob(jobKey)
        .WithIdentity($"{nameof(EvaluationWorkerJob)}-trigger")
        .WithSimpleSchedule(schedule => schedule
            .WithInterval(TimeSpan.FromSeconds(10))
            .RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
    var migrations = dbContext.Database.GetMigrations();
    if (migrations.Any())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    await DataSeeder.SeedAsync(dbContext, CancellationToken.None);
    var searchService = scope.ServiceProvider.GetRequiredService<IComplianceSearchService>();
    await searchService.EnsureIndexExistsAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AGONECompliance.Client._Imports).Assembly);

app.Run();
