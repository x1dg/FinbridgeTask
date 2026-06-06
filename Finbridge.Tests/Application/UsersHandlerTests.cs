using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Application.Users.Commands;
using Finbridge.Application.Users.Queries;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Repositories;
using Finbridge.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Tests.Application;

public class UsersHandlerTests
{
    private static (IRequestDispatcher dispatcher, FinbridgeDbContext ctx) Build()
    {
        var dbName = $"UsersTestDb_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var repo = new UserRepository(ctx);

        var services = new ServiceCollection();
        services.AddScoped<IUserRepository>(_ => repo);
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<IRequestHandler<CreateUserRequest, UserResponse>, CreateUserHandler>();
        services.AddScoped<IRequestHandler<GetUserByIdQuery, UserResponse?>, GetUserByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserResponse>>, GetAllUsersHandler>();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IRequestDispatcher>(), ctx);
    }

    [Fact]
    public async Task CreateUser_ShouldCreateUserWithZeroBalance()
    {
        var (dispatcher, _) = Build();
        var user = await dispatcher.SendAsync(new CreateUserRequest(
            "Иван Иванов", new DateTime(1990, 1, 1), "Москва"));

        Assert.Equal("Иван Иванов", user.FullName);
        Assert.Equal(0m, user.Balance);
        Assert.Equal((uint)0, user.Version);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser_WhenExists()
    {
        var (dispatcher, _) = Build();
        var created = await dispatcher.SendAsync(new CreateUserRequest(
            "Пётр", new DateTime(1991, 1, 1), "СПб"));

        var fetched = await dispatcher.SendAsync(new GetUserByIdQuery(created.Id));
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNull_WhenMissing()
    {
        var (dispatcher, _) = Build();
        Assert.Null(await dispatcher.SendAsync(new GetUserByIdQuery(999)));
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnAllUsers()
    {
        var (dispatcher, _) = Build();
        await dispatcher.SendAsync(new CreateUserRequest("А", new DateTime(1990, 1, 1), "X"));
        await dispatcher.SendAsync(new CreateUserRequest("Б", new DateTime(1990, 1, 1), "Y"));

        var users = await dispatcher.SendAsync(new GetAllUsersQuery());
        Assert.Equal(2, users.Count);
    }
}
