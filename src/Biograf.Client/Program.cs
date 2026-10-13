using Biograf.Client;
using Biograf.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API:t ligger på en annan port än klienten. Kör klienten på http används API:ts
// http-adress och tvärtom, se wwwroot/appsettings.json.
var schema = new Uri(builder.HostEnvironment.BaseAddress).Scheme;
var apiUrl = builder.Configuration[$"ApiUrl:{schema}"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });

builder.Services.AddScoped<TokenLagring>();
builder.Services.AddScoped<ApiKlient>();

// Inloggning: JwtAuthStateProvider läser rollerna ur token så att menyer och sidor kan anpassas.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

await builder.Build().RunAsync();
