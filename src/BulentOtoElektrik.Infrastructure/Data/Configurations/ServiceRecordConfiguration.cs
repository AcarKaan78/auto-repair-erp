using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BulentOtoElektrik.Infrastructure.Data.Configurations;

public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRecord> builder)
    {
        builder.HasKey(sr => sr.Id);
        builder.Property(sr => sr.Complaint).HasMaxLength(500);
        builder.Property(sr => sr.WorkPerformed).IsRequired().HasMaxLength(500);
        builder.Property(sr => sr.UnitPrice).HasColumnType("REAL");
        builder.Property(sr => sr.TotalAmount).HasColumnType("REAL");
        builder.Property(sr => sr.Notes).HasMaxLength(1000);
        builder.Property(sr => sr.Currency)
            .HasConversion<string>()
            .HasDefaultValue(CurrencyType.TL);

        builder.HasIndex(sr => sr.ServiceDate);
        builder.HasIndex(sr => sr.VehicleId);

        builder.HasOne(sr => sr.Technician)
            .WithMany(t => t.ServiceRecords)
            .HasForeignKey(sr => sr.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sr => sr.StockItem)
            .WithMany(si => si.ServiceRecords)
            .HasForeignKey(sr => sr.StockItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
