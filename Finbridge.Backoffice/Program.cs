using AntDesign;
using Finbridge.Application;
using Finbridge.Application.Configuration;
using Finbridge.Data;
using Finbridge.Backoffice.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BalanceSettings>(builder.Configuration.GetSection("BalanceSettings"));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAntDesign();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<ThemeService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
