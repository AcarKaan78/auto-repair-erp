using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BulentOtoElektrik.Infrastructure.Data.Configurations;

public class DailyExpenseConfiguration : IEntityTypeConfiguration<DailyExpense>
{
    public void Configure(EntityTypeBuilder<DailyExpense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasColumnType("REAL");
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.Currency)
            .HasConversion<string>()
            .HasDefaultValue(CurrencyType.TL);

        builder.HasIndex(e => e.ExpenseDate);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
