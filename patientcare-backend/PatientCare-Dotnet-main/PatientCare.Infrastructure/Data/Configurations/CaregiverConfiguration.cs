using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class CaregiverConfiguration : IEntityTypeConfiguration<Caregiver>
{
    public void Configure(EntityTypeBuilder<Caregiver> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.PasswordHash)
            .IsRequired();

        builder.Property(c => c.CaregiverGender)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(c => c.Assignments)
            .WithOne(a => a.Caregiver)
            .HasForeignKey(a => a.CaregiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
