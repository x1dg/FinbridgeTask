using Finbridge.Application.Contracts;
using Finbridge.Application.Services;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Finbridge.Tests.Application;

public class UserServiceTests
{
    private static (UserService svc, FinbridgeDbContext ctx) Build()
    {
        var dbName = $"UserServiceTestDb_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var repo = new UserRepository(ctx);
        return (new UserService(repo), ctx);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUserWithZeroBalance()
    {
        var (svc, _) = Build();
        var user = await svc.CreateAsync(new CreateUserRequest(
            "Иван Иванов", new DateTime(1990, 1, 1), "Москва"));

        Assert.Equal("Иван Иванов", user.FullName);
        Assert.Equal(0m, user.Balance);
        Assert.Equal((uint)0, user.Version);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        var (svc, _) = Build();
        var created = await svc.CreateAsync(new CreateUserRequest(
            "Пётр", new DateTime(1991, 1, 1), "СПб"));

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMissing()
    {
        var (svc, _) = Build();
        Assert.Null(await svc.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        var (svc, _) = Build();
        await svc.CreateAsync(new CreateUserRequest("А", new DateTime(1990, 1, 1), "X"));
        await svc.CreateAsync(new CreateUserRequest("Б", new DateTime(1990, 1, 1), "Y"));

        var users = await svc.GetAllAsync();
        Assert.Equal(2, users.Count);
    }
}
