using PatientCare.Core.Enums;

namespace PatientCare.Core.Entities;

public class Caregiver // בן משפחה
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Gender CaregiverGender { get; set; }
    public DateTime DateOfBirth { get; set; }

    /// <summary>תפקיד כללי במערכת. ברירת מחדל: User.</summary>
    public CaregiverRole Role { get; set; } = CaregiverRole.User;

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();

    public Gender GetGender() => CaregiverGender;

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

    /// <summary>True when the caregiver's gender satisfies a shift's required gender (Any always matches).</summary>
    public bool IsMatchingGender(GenderRequirement requiredGender)
    {
        return requiredGender switch
        {
            GenderRequirement.Any => true,
            GenderRequirement.Male => CaregiverGender == Gender.Male,
            GenderRequirement.Female => CaregiverGender == Gender.Female,
            _ => false
        };
    }
}
