using Finbridge.Data;
using Finbridge.Api.Services;
using Finbridge.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MVC services
builder.Services.AddControllersWithViews();

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<FinbridgeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services for balance settings
builder.Services.Configure<BalanceSettings>(
    builder.Configuration.GetSection("BalanceSettings"));

// Add services for Kafka settings
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("KafkaSettings"));

// Register services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();

// Rate limiting configuration
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10; // 10 requests
        options.Window = TimeSpan.FromSeconds(10); // per 10 seconds
        options.QueueLimit = 5; // allow 5 requests to be queued
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    }));

var app = builder.Build();

// Ensure database is created (for demo / Docker first-run)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinbridgeDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom exception handling middleware
app.UseExceptionHandling();

// Rate limiting middleware
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
