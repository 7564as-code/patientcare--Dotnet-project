using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class ShiftSwapRequestConfiguration : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.ResponseReason)
            .HasMaxLength(500);

        builder.HasOne(r => r.Requestor)
            .WithMany()
            .HasForeignKey(r => r.RequestorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.TargetCareShift)
    .WithMany()
    .HasForeignKey(r => r.TargetCareShiftId)
    .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TargetCaregiver)
            .WithMany()
            .HasForeignKey(r => r.TargetCaregiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CareShift)
            .WithMany()
            .HasForeignKey(r => r.CareShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
