using Finbridge.Data.Configurations;
using Finbridge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Finbridge.Data;

public sealed class FinbridgeDbContext : DbContext
{
    public FinbridgeDbContext(DbContextOptions<FinbridgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BalanceHistory> BalanceHistories => Set<BalanceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new BalanceHistoryConfiguration());
    }
}
