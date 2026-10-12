using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;

namespace PatientCare.Core.Interfaces;

public interface ICaregiverRepository
{
    /// <summary>מחזיר בן משפחה (עם מעקב שינויים).</summary>
    Task<Caregiver?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Caregiver?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>כל בני המשפחה בעימוד אמיתי מול בסיס הנתונים (למנהל מערכת).</summary>
    Task<PagedResult<Caregiver>> GetPagedAsync(PageQuery query, CancellationToken ct = default);

    void Add(Caregiver caregiver);
}
