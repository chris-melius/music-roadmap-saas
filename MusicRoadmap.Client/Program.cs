using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MusicRoadmap.Client;
using MusicRoadmap.Client.Pages;
using MusicRoadmap.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MusicRoadmap.Client.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using MusicRoadmap.Client.Security;
using System.ComponentModel.DataAnnotations;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5213/";

// 1. Register our custom provider implementation explicitly
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// 2. Map standard Blazor core Auth abstractions to resolve using our implementation instance
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CustomAuthenticationStateProvider>());

builder.Services.AddAuthorizationCore();

// Load logging configuration from wwwroot/appsettings.json
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Ensure the minimum level is set to capture your Console.Write calls
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LocalStorageService>();

// 1. Register our class as itself (so we can call Notify methods)
builder.Services.AddScoped<CustomAuthStateProvider>();

// 2. Register it as the official AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CustomAuthStateProvider>());

// 1. Register the handler
builder.Services.AddTransient<JwtInterceptorHandler>();

// 2. Configure the HttpClient to use the handler
//builder.Services.AddHttpClient("Api", client => 
//{
  //  client.BaseAddress = new Uri(apiBaseUrl);
//})
//.AddHttpMessageHandler<JwtAuthorizationHandler>();

// 1. Establish the API base string
// moved to top of file

// 2. REGISTER ANONYMOUS CLIENT: Dedicated named client with NO handlers attached
builder.Services.AddHttpClient("AnonymousClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// 3. REGISTER DEFAULT FALLBACK CLIENT: Used for relative paths inside Login/Register components
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl) 
});

// 4. REGISTER THE GATEKEEPER INTERCEPTOR MIDDLEWARE
builder.Services.AddTransient<JwtInterceptorHandler>();

// 5. REGISTER SECURED SERVICES: StudentService automatically gets an Interceptor-wrapped client pipeline
builder.Services.AddHttpClient("SecureApi", client => 
{ 
    client.BaseAddress = new Uri(apiBaseUrl); 
})
.AddHttpMessageHandler<JwtInterceptorHandler>();

// 4. REGISTER THE SECURED STUDENT SERVICE
builder.Services.AddHttpClient<StudentService>(client => 
{ 
    client.BaseAddress = new Uri(apiBaseUrl); 
})
.AddHttpMessageHandler<JwtInterceptorHandler>();

// 6. Map Auth States
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
