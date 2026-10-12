using PatientCare.Core.Enums;

namespace PatientCare.Core.Entities;

public class CareShift
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>Gender the assigned caregiver(s) must match; Any means no restriction.</summary>
    public GenderRequirement RequiredGender { get; set; } = GenderRequirement.Any;

    public int Capacity { get; set; }
    public int CurrentCount { get; set; }

    /// <summary>Optimistic concurrency token guarding CurrentCount against concurrent registrations.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public TimeSpan GetDuration() => EndTime - StartTime;

    public bool IsFull() => CurrentCount >= Capacity;

    /// <summary>Checks both remaining capacity and gender-matching eligibility for the given caregiver.</summary>
    public bool CanAssignCaregiver(Caregiver caregiver)
    {
        if (caregiver is null)
        {
            return false;
        }

        return !IsFull() && caregiver.IsMatchingGender(RequiredGender);
    }
}
