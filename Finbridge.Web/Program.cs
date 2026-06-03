using Microsoft.Extensions.Options;
using Finbridge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure HttpClient for API service
builder.Services.AddHttpClient<Finbridge.Web.Services.ApiService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<Finbridge.Web.Services.ApiSettings>>();
    client.BaseAddress = new Uri(settings.Value.BaseUrl);
});

// Load API settings
builder.Services.Configure<Finbridge.Web.Services.ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

// Register ApiService
builder.Services.AddScoped<Finbridge.Web.Services.ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
