using PatientCare.Core.Enums;

namespace PatientCare.Core.Entities;

public class ShiftSwapRequest
{
    public int Id { get; set; }

    public int RequestorId { get; set; }
    public Caregiver Requestor { get; set; } = null!;

    public int? TargetCaregiverId { get; set; }
    public Caregiver? TargetCaregiver { get; set; }

    public int CareShiftId { get; set; }
    public int TargetCareShiftId { get; set; }
    public CareShift CareShift { get; set; } = null!;
    public CareShift TargetCareShift { get; set; } = null!;
    public SwapRequestStatus Status { get; set; } = SwapRequestStatus.Pending;
    public string? Reason { get; set; }

    /// <summary>סיבת הדחייה (אופציונלית), נכתבת על ידי בן המשפחה שדחה את הבקשה.</summary>
    public string? ResponseReason { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
