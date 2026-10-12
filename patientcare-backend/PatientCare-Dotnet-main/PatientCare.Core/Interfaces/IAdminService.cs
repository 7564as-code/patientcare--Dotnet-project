using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Enums;

namespace PatientCare.Core.Interfaces;

/// <summary>פעולות למנהל מערכת (Role = Admin) בלבד.</summary>
public interface IAdminService
{
    Task<PagedResult<CaregiverSummaryResponse>> GetCaregiversAsync(PageQuery query, CancellationToken ct = default);

    Task<PagedResult<PatientResponse>> GetAllPatientsAsync(PatientQuery query, int adminId, CancellationToken ct = default);

    Task<ServiceResult<CaregiverSummaryResponse>> ChangeRoleAsync(int caregiverId, CaregiverRole role, int actingAdminId, CancellationToken ct = default);
}
