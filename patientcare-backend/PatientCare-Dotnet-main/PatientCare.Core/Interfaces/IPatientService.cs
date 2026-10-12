using PatientCare.Core.Common;
using PatientCare.Core.DTOs;

namespace PatientCare.Core.Interfaces;

public interface IPatientService
{
    /// <summary>החולים שבן המשפחה מנהל או שהצטרף למעגל שלהם (עימוד, חיפוש ומיון בבסיס הנתונים).</summary>
    Task<PagedResult<PatientResponse>> GetPatientsAsync(int caregiverId, PatientQuery query, CancellationToken ct = default);

    /// <summary>מקומות הטיפול של החולים הגלויים לבן המשפחה (לסינון).</summary>
    Task<List<string>> GetDepartmentsAsync(int caregiverId, CancellationToken ct = default);

    Task<ServiceResult<PatientResponse>> GetPatientByIdAsync(int id, int caregiverId, CancellationToken ct = default);

    Task<ServiceResult<PatientResponse>> CreatePatientAsync(
        CreatePatientRequest request,
        int managerCaregiverId,
        CancellationToken ct = default);

    /// <summary>העברת ניהול: רק המנהל הנוכחי, ורק לבן משפחה שכבר חבר במעגל.</summary>
    Task<ServiceResult<PatientResponse>> ChangeManagerAsync(
        int patientId,
        int currentManagerId,
        int newManagerCaregiverId,
        CancellationToken ct = default);

    /// <summary>הצטרפות למעגל של חולה באמצעות קוד הזמנה.</summary>
    Task<ServiceResult<PatientResponse>> JoinAsync(string inviteCode, int caregiverId, CancellationToken ct = default);

    Task<ServiceResult<List<PatientMemberResponse>>> GetMembersAsync(int patientId, int caregiverId, CancellationToken ct = default);

    /// <summary>קוד ההזמנה של החולה (למנהל בלבד). reset=true יוצר קוד חדש ומבטל את הישן.</summary>
    Task<ServiceResult<InviteCodeResponse>> GetInviteCodeAsync(int patientId, int caregiverId, bool reset, CancellationToken ct = default);
}
