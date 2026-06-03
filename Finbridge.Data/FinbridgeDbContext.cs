using Finbridge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Finbridge.Data
{
    public class FinbridgeDbContext : DbContext
    {
        public FinbridgeDbContext(DbContextOptions<FinbridgeDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<BalanceHistory> BalanceHistories => Set<BalanceHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PlaceOfBirth)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Version)
                    .IsRequired()
                    .IsConcurrencyToken();
            });

            modelBuilder.Entity<BalanceHistory>(entity =>
            {
                entity.Property(e => e.ChangedAt)
                    .IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.BalanceHistory)
                    .HasForeignKey(d => d.UserId);
            });
        }
    }
}