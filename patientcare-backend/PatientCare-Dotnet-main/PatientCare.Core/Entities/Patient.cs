using PatientCare.Core.Enums;

namespace PatientCare.Core.Entities;

public class Patient
{
    public int Id { get; set; }
    public string MedicalCode { get; set; } = string.Empty;
    public Gender PatientGender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Department { get; set; } = string.Empty;

    public int? ManagerCaregiverId { get; set; }

    /// <summary>קוד הזמנה שבו בני משפחה מצטרפים למעגל של החולה. נוצר אוטומטית.</summary>
    public string? InviteCode { get; set; }

    public Caregiver? ManagerCaregiver { get; set; }
    public ICollection<CareShift> CareShifts { get; set; } = new List<CareShift>();
    public ICollection<PatientMember> Members { get; set; } = new List<PatientMember>();

    public Gender GetGender() => PatientGender;

    public int GetAge()
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    public AgeCategory GetAgeCategory()
    {
        var age = GetAge();
        return age switch
        {
            < 18 => AgeCategory.Child,
            < 65 => AgeCategory.Adult,
            _ => AgeCategory.Senior
        };
    }
}
