using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        // שם/כינוי החולה. אינו ייחודי גלובלית: משפחות שונות יכולות לקרוא לחולה "אמא".
        builder.Property(p => p.MedicalCode)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(p => p.MedicalCode);

        builder.HasOne(p => p.ManagerCaregiver)
            .WithMany()
            .HasForeignKey(p => p.ManagerCaregiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.InviteCode)
            .HasMaxLength(16);
        builder.HasIndex(p => p.InviteCode).IsUnique();

        builder.Property(p => p.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PatientGender)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.HasMany(p => p.CareShifts)
            .WithOne(s => s.Patient)
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
