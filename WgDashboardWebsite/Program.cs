using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;
using WgDashboardWebsite;
using WgDashboardWebsite.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string baseUrl = builder.Configuration.GetSection("WireguardApiConfig").GetValue<string>("BaseUrl") ?? "http://localhost:3000";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

builder.Services.AddAuthorizationCore();

builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
