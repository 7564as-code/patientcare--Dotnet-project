namespace PatientCare.Core.Entities;

public class ShiftAssignment
{
    public int Id { get; set; }

    public int CareShiftId { get; set; }
    public CareShift CareShift { get; set; } = null!;

    public int CaregiverId { get; set; }
    public Caregiver Caregiver { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
