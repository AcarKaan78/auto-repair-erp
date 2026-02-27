using BulentOtoElektrik.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BulentOtoElektrik.Infrastructure.Data.Configurations;

public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.FullName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Phone).HasMaxLength(20);
    }
}
