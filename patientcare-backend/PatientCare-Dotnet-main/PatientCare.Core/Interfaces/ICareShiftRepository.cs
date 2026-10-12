using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;

namespace PatientCare.Core.Interfaces;

public interface ICareShiftRepository
{
    /// <summary>משמרות של חולים שבן המשפחה חבר במעגל שלהם, בעימוד אמיתי, כולל חולה/תגיות/משובצים.</summary>
    Task<PagedResult<CareShift>> GetVisiblePagedAsync(int caregiverId, CareShiftQuery query, CancellationToken ct = default);

    /// <summary>משמרת גלויה לבן המשפחה, לקריאה בלבד.</summary>
    Task<CareShift?> GetVisibleByIdAsync(int id, int caregiverId, CancellationToken ct = default);

    /// <summary>משמרת עם חולה, תגיות ומשובצים, עם מעקב שינויים (לשיבוץ).</summary>
    Task<CareShift?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);

    Task<CareShift?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>משמרת עם המשובצים בה, עם מעקב שינויים.</summary>
    Task<CareShift?> GetByIdWithAssignmentsAsync(int id, CancellationToken ct = default);

    /// <summary>האם יש לחולה משמרת שחופפת בזמן לטווח הנתון.</summary>
    Task<bool> HasOverlapAsync(int patientId, DateTime start, DateTime end, CancellationToken ct = default);

    Task<List<Tag>> GetTagsByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default);

    Task<ShiftAssignment?> GetAssignmentAsync(int shiftId, int caregiverId, CancellationToken ct = default);

    Task<bool> IsAssignedAsync(int shiftId, int caregiverId, CancellationToken ct = default);

    void Add(CareShift shift);
    void AddTag(Tag tag);
}
