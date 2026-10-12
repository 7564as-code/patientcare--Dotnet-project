using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;

namespace PatientCare.Core.Interfaces;

public interface IPatientRepository
{
    /// <summary>החולים שבן המשפחה מנהל או חבר במעגל שלהם, בעימוד אמיתי (ללא מעקב).</summary>
    Task<PagedResult<Patient>> GetVisiblePagedAsync(int caregiverId, PatientQuery query, CancellationToken ct = default);

    /// <summary>כל החולים במערכת (למנהל מערכת בלבד).</summary>
    Task<PagedResult<Patient>> GetAllPagedAsync(PatientQuery query, CancellationToken ct = default);

    /// <summary>מקומות הטיפול השונים של החולים הגלויים לבן המשפחה.</summary>
    Task<List<string>> GetVisibleDepartmentsAsync(int caregiverId, CancellationToken ct = default);

    /// <summary>חולה גלוי לבן המשפחה, לקריאה בלבד.</summary>
    Task<Patient?> GetVisibleByIdAsync(int id, int caregiverId, CancellationToken ct = default);

    /// <summary>חולה גלוי לבן המשפחה, עם מעקב שינויים (לעדכון).</summary>
    Task<Patient?> GetVisibleByIdTrackedAsync(int id, int caregiverId, CancellationToken ct = default);

    /// <summary>חולה עם רשימת החברים במעגל, עם מעקב שינויים.</summary>
    Task<Patient?> GetByIdWithMembersAsync(int id, CancellationToken ct = default);

    /// <summary>חולה גלוי עם המנהל והחברים (כולל שמותיהם), לקריאה בלבד.</summary>
    Task<Patient?> GetVisibleWithMembersDetailsAsync(int id, int caregiverId, CancellationToken ct = default);

    Task<Patient?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);

    Task<bool> ManagerHasPatientNamedAsync(int managerCaregiverId, string name, CancellationToken ct = default);

    Task<bool> InviteCodeExistsAsync(string inviteCode, CancellationToken ct = default);

    /// <summary>האם בן המשפחה מנהל את החולה או חבר במעגל שלו.</summary>
    Task<bool> IsMemberAsync(int patientId, int caregiverId, CancellationToken ct = default);

    /// <summary>האם קיימת שורת חברות מפורשת (לא כולל המנהל).</summary>
    Task<bool> HasMembershipRowAsync(int patientId, int caregiverId, CancellationToken ct = default);

    void Add(Patient patient);
    void AddMember(PatientMember member);
    void RemoveMember(PatientMember member);
}
