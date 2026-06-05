using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Application.Users.Commands;
using Finbridge.Application.Users.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        services.AddScoped<IRequestHandler<CreateUserRequest, UserResponse>, CreateUserHandler>();
        services.AddScoped<IRequestHandler<UpdateBalanceRequest, UserResponse>, UpdateBalanceHandler>();
        services.AddScoped<IRequestHandler<BatchUpdateBalancesRequest, Unit>, BatchUpdateBalancesHandler>();
        services.AddScoped<IRequestHandler<GetUserByIdQuery, UserResponse?>, GetUserByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserResponse>>, GetAllUsersHandler>();
        services.AddScoped<IRequestHandler<GetBalanceHistoryQuery, IReadOnlyList<BalanceHistoryResponse>>, GetBalanceHistoryHandler>();

        return services;
    }
}
