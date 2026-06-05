using Finbridge.Domain.Users;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finbridge.Data.Configurations;

public sealed class BalanceHistoryConfiguration : IEntityTypeConfiguration<BalanceHistory>
{
    public void Configure(EntityTypeBuilder<BalanceHistory> builder)
    {
        builder.ToTable("balance_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();

        builder.Property(h => h.UserId).IsRequired();

        builder.Property(h => h.Delta)
            .HasConversion(v => v.Amount, v => Money.DeltaOf(v))
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(h => h.NewBalance)
            .HasConversion(v => v.Amount, v => Money.Of(v))
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(h => h.ChangedAt).IsRequired();
    }
}
