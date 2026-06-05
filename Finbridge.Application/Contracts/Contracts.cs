using Finbridge.Application.Abstractions;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Application.Contracts;

public sealed record CreateUserRequest(
    string FullName,
    DateTime DateOfBirth,
    string PlaceOfBirth) : ICommand<UserResponse>;

public sealed record UpdateBalanceRequest(
    int UserId,
    decimal Amount) : ICommand<UserResponse>;

public sealed record BatchUpdateBalancesRequest(
    IReadOnlyList<UpdateBalanceRequest> Updates) : ICommand<Unit>;

public sealed record GetUserByIdQuery(int Id) : IQuery<UserResponse?>;

public sealed record GetAllUsersQuery() : IQuery<IReadOnlyList<UserResponse>>;

public sealed record GetBalanceHistoryQuery(int UserId, int Limit = 20) : IQuery<IReadOnlyList<BalanceHistoryResponse>>;

public sealed record UserResponse(
    int Id,
    string FullName,
    DateTime DateOfBirth,
    string PlaceOfBirth,
    decimal Balance,
    uint Version);

public sealed record BalanceHistoryResponse(
    int Id,
    int UserId,
    decimal AmountChanged,
    decimal NewBalance,
    DateTime ChangedAt);

public static class UserMappings
{
    public static UserResponse ToResponse(this Domain.Users.User user) =>
        new(
            user.Id,
            user.FullName.Value,
            user.DateOfBirth,
            user.PlaceOfBirth,
            user.Balance.Amount,
            user.Version);

    public static BalanceHistoryResponse ToResponse(this Domain.Users.BalanceHistory h) =>
        new(h.Id, h.UserId, h.Delta.Amount, h.NewBalance.Amount, h.ChangedAt);

    public static IReadOnlyList<UserResponse> ToResponses(this IEnumerable<Domain.Users.User> users) =>
        users.Select(u => u.ToResponse()).ToList();

    public static IReadOnlyList<BalanceHistoryResponse> ToResponses(this IEnumerable<Domain.Users.BalanceHistory> history) =>
        history.Select(h => h.ToResponse()).ToList();
}
