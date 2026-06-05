using System.Threading.RateLimiting;
using Finbridge.Api.Events;
using Finbridge.Api.Middleware;
using Finbridge.Api.Services;
using Finbridge.Application;
using Finbridge.Application.Events;
using Finbridge.Application.Services;
using Finbridge.Data;
using Finbridge.Domain.Users.Events;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BalanceSettings>(builder.Configuration.GetSection("BalanceSettings"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
builder.Services.AddScoped<IDomainEventHandler<BalanceUpdatedDomainEvent>, BalanceUpdatedKafkaHandler>();

builder.Services.AddApplication();

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueLimit = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinbridgeDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandling();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
