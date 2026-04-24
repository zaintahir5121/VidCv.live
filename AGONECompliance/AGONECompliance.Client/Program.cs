using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AGONECompliance.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<PortalContext>();

await builder.Build().RunAsync();
