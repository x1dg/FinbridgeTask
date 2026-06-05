namespace Finbridge.Domain.Users.Repositories;

/// <summary>
/// Контракт репозитория для агрегата User. Реализация живёт
/// в инфраструктурном слое (Finbridge.Data).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BalanceHistory>> GetBalanceHistoryAsync(int userId, int limit, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет все накопленные изменения в агрегате.
    /// Бросает <see cref="Exceptions.ConcurrencyConflictException"/> при конфликте версий.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
