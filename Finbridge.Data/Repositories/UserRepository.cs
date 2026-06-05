using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Finbridge.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly FinbridgeDbContext _context;

    public UserRepository(FinbridgeDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BalanceHistory>> GetBalanceHistoryAsync(int userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.BalanceHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ChangedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }
}
