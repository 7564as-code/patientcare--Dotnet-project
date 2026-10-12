using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data.Configurations;

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        // A caregiver may only hold one assignment per shift.
        builder.HasIndex(a => new { a.CareShiftId, a.CaregiverId }).IsUnique();
    }
}
