using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class CareShiftConfiguration : IEntityTypeConfiguration<CareShift>
{
    public void Configure(EntityTypeBuilder<CareShift> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RequiredGender)
            .HasConversion<string>()
            .HasMaxLength(10);

        // Concurrency token guarding CurrentCount against races on the last available slot.
        // הערך נקבע באפליקציה (AppDbContext.SaveChanges) ולא על ידי בסיס הנתונים, כדי שיעבוד אותו דבר
        // ב-PostgreSQL (אין שם rowversion אוטומטי) ובבדיקות.
        builder.Property(s => s.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasMany(s => s.Assignments)
            .WithOne(a => a.CareShift)
            .HasForeignKey(a => a.CareShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Tags)
            .WithMany(t => t.CareShifts)
            .UsingEntity(j => j.ToTable("CareShiftTags"));
    }
}
