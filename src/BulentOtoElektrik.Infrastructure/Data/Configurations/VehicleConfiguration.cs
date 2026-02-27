using BulentOtoElektrik.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BulentOtoElektrik.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.PlateNumber).IsRequired().HasMaxLength(20);
        builder.Property(v => v.VehicleModel).HasMaxLength(100);
        builder.Property(v => v.VehicleBrand).HasMaxLength(100);
        builder.Property(v => v.Notes).HasMaxLength(1000);

        builder.HasIndex(v => v.PlateNumber).IsUnique();
        builder.HasIndex(v => v.CustomerId);

        builder.HasMany(v => v.ServiceRecords)
            .WithOne(sr => sr.Vehicle)
            .HasForeignKey(sr => sr.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
