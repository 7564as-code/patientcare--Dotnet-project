using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class PatientMemberConfiguration : IEntityTypeConfiguration<PatientMember>
{
    public void Configure(EntityTypeBuilder<PatientMember> builder)
    {
        builder.HasKey(m => m.Id);

        // בן משפחה מופיע פעם אחת בלבד במעגל של חולה.
        builder.HasIndex(m => new { m.PatientId, m.CaregiverId }).IsUnique();

        builder.HasOne(m => m.Patient)
            .WithMany(p => p.Members)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Caregiver)
            .WithMany()
            .HasForeignKey(m => m.CaregiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
