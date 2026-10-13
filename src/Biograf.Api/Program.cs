using Biograf.Api.Data;
using Biograf.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Blazor-klienten körs på en egen port och behöver därför tillåtas via CORS.
const string KlientPolicy = "Klient";
var klientUrl = builder.Configuration["KlientUrl"] ?? "https://localhost:7071";

builder.Services.AddCors(options =>
    options.AddPolicy(KlientPolicy, policy => policy
        .WithOrigins(klientUrl, "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddDbContext<BiografDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Biograf")));

// Identity sköter användare, lösenordshashning och roller.
builder.Services
    .AddIdentityCore<Anvandare>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BiografDbContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Skapa/uppdatera databasen och fyll den med testdata vid uppstart.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BiografDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(KlientPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();

// Gör Program synlig för integrationstester (WebApplicationFactory).
public partial class Program { }
