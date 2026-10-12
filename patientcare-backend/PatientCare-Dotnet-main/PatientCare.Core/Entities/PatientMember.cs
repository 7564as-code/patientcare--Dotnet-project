namespace PatientCare.Core.Entities;

/// <summary>
/// חברות של בן משפחה במעגל של חולה. המנהל (Patient.ManagerCaregiverId) נחשב חבר באופן מובלע
/// ולכן אינו חייב שורה בטבלה זו.
/// </summary>
public class PatientMember
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int CaregiverId { get; set; }
    public Caregiver Caregiver { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
