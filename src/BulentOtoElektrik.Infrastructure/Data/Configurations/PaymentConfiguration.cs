using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BulentOtoElektrik.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType("REAL");
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.Currency)
            .HasConversion<string>()
            .HasDefaultValue(CurrencyType.TL);
        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasDefaultValue(PaymentMethod.Cash);

        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.Vehicle)
            .WithMany()
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
