using Finbridge.Web.Auth;
using Finbridge.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddTransient<TokenAuthHandler>();

builder.Services.AddHttpClient("auth-bootstrap", (sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHttpClient<Finbridge.Web.Services.ApiService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
}).AddHttpMessageHandler<TokenAuthHandler>();

builder.Services.Configure<Finbridge.Web.Services.ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddScoped<Finbridge.Web.Services.ApiService>();

builder.Services.AddHostedService<AuthBootstrapService>();

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
