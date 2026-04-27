using Experion.Backend.Options;
using Experion.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.Configure<ExperionModuleOptions>(
    builder.Configuration.GetSection(ExperionModuleOptions.SectionName));
builder.Services.AddHttpClient(nameof(OpenAiSuggestionClient), client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient(nameof(FacebookActionExecutor), client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddSingleton<ITokenEstimator, SimpleTokenEstimator>();
builder.Services.AddSingleton<IDomSanitizer, LlmDomSanitizer>();
builder.Services.AddSingleton<IIntentRouter, IntentRouter>();
builder.Services.AddSingleton<IActionKnowledgeBaseRepository, InMemoryActionKnowledgeBaseRepository>();
builder.Services.AddSingleton<IConversationMemoryRepository, InMemoryConversationMemoryRepository>();
builder.Services.AddSingleton<ILearningCache, AzureSearchLearningCache>();
builder.Services.AddScoped<IOpenAiSuggestionClient, OpenAiSuggestionClient>();
builder.Services.AddScoped<IActionExecutor, FacebookActionExecutor>();
builder.Services.AddScoped<IExperionOrchestrator, ExperionOrchestrator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.Run();
