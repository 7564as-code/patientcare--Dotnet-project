using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Core.Interfaces;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _db;

    public PatientRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<Patient>> GetVisiblePagedAsync(int caregiverId, PatientQuery query, CancellationToken ct = default) =>
        PageAsync(_db.Patients.AsNoTracking().VisibleTo(caregiverId), query, ct);

    public Task<PagedResult<Patient>> GetAllPagedAsync(PatientQuery query, CancellationToken ct = default) =>
        PageAsync(_db.Patients.AsNoTracking(), query, ct);

    /// <summary>סינון, מיון ועימוד (Skip/Take) מתבצעים כולם בתוך השאילתה מול בסיס הנתונים.</summary>
    private static async Task<PagedResult<Patient>> PageAsync(IQueryable<Patient> source, PatientQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower כדי שהחיפוש לא יהיה תלוי באותיות גדולות/קטנות גם ב-PostgreSQL.
            var term = query.Search.Trim().ToLower();
            source = source.Where(p => p.MedicalCode.ToLower().Contains(term) || p.Department.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = query.Department.Trim();
            source = source.Where(p => p.Department == department);
        }

        var total = await source.CountAsync(ct);

        // גיל עולה = תאריך לידה יורד.
        IOrderedQueryable<Patient> ordered = query.SortBy switch
        {
            PatientSortBy.Department => query.Descending
                ? source.OrderByDescending(p => p.Department)
                : source.OrderBy(p => p.Department),
            PatientSortBy.Age => query.Descending
                ? source.OrderBy(p => p.DateOfBirth)
                : source.OrderByDescending(p => p.DateOfBirth),
            _ => query.Descending
                ? source.OrderByDescending(p => p.MedicalCode)
                : source.OrderBy(p => p.MedicalCode)
        };

        var items = await ordered
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Patient>(items, query.Page, query.PageSize, total);
    }

    public Task<List<string>> GetVisibleDepartmentsAsync(int caregiverId, CancellationToken ct = default) =>
        _db.Patients
            .AsNoTracking()
            .VisibleTo(caregiverId)
            .Select(p => p.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);

    public Task<Patient?> GetVisibleByIdAsync(int id, int caregiverId, CancellationToken ct = default) =>
        _db.Patients
            .AsNoTracking()
            .VisibleTo(caregiverId)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Patient?> GetVisibleByIdTrackedAsync(int id, int caregiverId, CancellationToken ct = default) =>
        _db.Patients
            .VisibleTo(caregiverId)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Patient?> GetByIdWithMembersAsync(int id, CancellationToken ct = default) =>
        _db.Patients
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Patient?> GetVisibleWithMembersDetailsAsync(int id, int caregiverId, CancellationToken ct = default) =>
        _db.Patients
            .AsNoTracking()
            .Include(p => p.ManagerCaregiver)
            .Include(p => p.Members).ThenInclude(m => m.Caregiver)
            .VisibleTo(caregiverId)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Patient?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default) =>
        _db.Patients.FirstOrDefaultAsync(p => p.InviteCode == inviteCode, ct);

    public Task<bool> ManagerHasPatientNamedAsync(int managerCaregiverId, string name, CancellationToken ct = default) =>
        _db.Patients.AnyAsync(p => p.ManagerCaregiverId == managerCaregiverId && p.MedicalCode == name, ct);

    public Task<bool> InviteCodeExistsAsync(string inviteCode, CancellationToken ct = default) =>
        _db.Patients.AnyAsync(p => p.InviteCode == inviteCode, ct);

    public Task<bool> IsMemberAsync(int patientId, int caregiverId, CancellationToken ct = default) =>
        _db.Patients.AnyAsync(p => p.Id == patientId
            && (p.ManagerCaregiverId == caregiverId || p.Members.Any(m => m.CaregiverId == caregiverId)), ct);

    public Task<bool> HasMembershipRowAsync(int patientId, int caregiverId, CancellationToken ct = default) =>
        _db.PatientMembers.AnyAsync(m => m.PatientId == patientId && m.CaregiverId == caregiverId, ct);

    public void Add(Patient patient) => _db.Patients.Add(patient);

    public void AddMember(PatientMember member) => _db.PatientMembers.Add(member);

    public void RemoveMember(PatientMember member) => _db.PatientMembers.Remove(member);
}
