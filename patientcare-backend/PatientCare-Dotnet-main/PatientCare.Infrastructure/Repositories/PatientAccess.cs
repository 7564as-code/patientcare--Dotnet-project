using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Repositories;

/// <summary>
/// כלל הגישה של המערכת: בן משפחה רואה רק חולים שהוא מנהל שלהם או שהצטרף למעגל שלהם.
/// </summary>
internal static class PatientAccess
{
    public static IQueryable<Patient> VisibleTo(this IQueryable<Patient> patients, int caregiverId) =>
        patients.Where(p => p.ManagerCaregiverId == caregiverId
                         || p.Members.Any(m => m.CaregiverId == caregiverId));

    public static IQueryable<CareShift> VisibleTo(this IQueryable<CareShift> shifts, int caregiverId) =>
        shifts.Where(s => s.Patient.ManagerCaregiverId == caregiverId
                       || s.Patient.Members.Any(m => m.CaregiverId == caregiverId));
}
