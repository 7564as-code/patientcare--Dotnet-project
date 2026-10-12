using PatientCare.Core.Common;
using PatientCare.Core.DTOs;

namespace PatientCare.Core.Interfaces;

public interface ICareShiftService
{
    /// <summary>משמרות של חולים שבן המשפחה מנהל או חבר במעגל שלהם (עימוד וסינון בבסיס הנתונים).</summary>
    Task<PagedResult<CareShiftResponse>> GetShiftsAsync(int caregiverId, CareShiftQuery query, CancellationToken ct = default);

    Task<ServiceResult<CareShiftResponse>> GetShiftByIdAsync(int id, int caregiverId, CancellationToken ct = default);

    /// <summary>יצירת משמרת: רק מנהל החולה, ולא בזמן שחופף למשמרת קיימת של אותו חולה.</summary>
    Task<ServiceResult<CareShiftResponse>> CreateShiftAsync(CreateCareShiftRequest request, int caregiverId, CancellationToken ct = default);

    /// <summary>
    /// משבץ את בן המשפחה (חבר במעגל של החולה) למקום פנוי במשמרת. בטוח לתחרות: מסתמך על RowVersion,
    /// תופס ConcurrencyException ומחזיר ServiceErrorType.Conflict (409).
    /// </summary>
    Task<ServiceResult<CareShiftResponse>> RegisterCaregiverForShiftAsync(int shiftId, int caregiverId, CancellationToken ct = default);
}
