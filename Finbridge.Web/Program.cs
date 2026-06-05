using Microsoft.Extensions.Options;
using Finbridge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<Finbridge.Web.Services.ApiService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<Finbridge.Web.Services.ApiSettings>>();
    client.BaseAddress = new Uri(settings.Value.BaseUrl);
});

builder.Services.Configure<Finbridge.Web.Services.ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddScoped<Finbridge.Web.Services.ApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
