using Finbridge.Domain.Users;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finbridge.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.FullName)
            .HasConversion(
                v => v.Value,
                v => FullName.Of(v))
            .IsRequired()
            .HasMaxLength(FullName.MaxLength);

        builder.Property(u => u.PlaceOfBirth)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Balance)
            .HasConversion(
                v => v.Amount,
                v => Money.Of(v))
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(u => u.Version)
            .IsRequired()
            .IsConcurrencyToken();

        var historyNav = builder.Metadata.FindNavigation(nameof(User.History))!;
        historyNav.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(typeof(BalanceHistory), nameof(User.History))
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
