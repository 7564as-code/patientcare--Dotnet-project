using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Interfaces;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Infrastructure.Repositories;

public class CareShiftRepository : ICareShiftRepository
{
    private readonly AppDbContext _db;

    public CareShiftRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>טוען בשאילתה אחת את החולה, התגיות והמשובצים (Include/ThenInclude), בלי שאילתה לכל משמרת (אין N+1).</summary>
    private IQueryable<CareShift> WithDetails() =>
        _db.CareShifts
            .Include(s => s.Patient)
            .Include(s => s.Tags)
            .Include(s => s.Assignments).ThenInclude(a => a.Caregiver);

    public async Task<PagedResult<CareShift>> GetVisiblePagedAsync(int caregiverId, CareShiftQuery query, CancellationToken ct = default)
    {
        var source = WithDetails().AsNoTracking().VisibleTo(caregiverId);

        if (query.PatientId.HasValue)
        {
            var patientId = query.PatientId.Value;
            source = source.Where(s => s.PatientId == patientId);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value;
            source = source.Where(s => s.EndTime > from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value;
            source = source.Where(s => s.StartTime < to);
        }

        if (query.OnlyWithOpenSlots)
        {
            source = source.Where(s => s.CurrentCount < s.Capacity);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            source = source.Where(s => s.Patient.MedicalCode.ToLower().Contains(term));
        }

        var total = await source.CountAsync(ct);

        var ordered = query.Descending
            ? source.OrderByDescending(s => s.StartTime)
            : source.OrderBy(s => s.StartTime);

        var items = await ordered
            .ThenBy(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<CareShift>(items, query.Page, query.PageSize, total);
    }

    public Task<CareShift?> GetVisibleByIdAsync(int id, int caregiverId, CancellationToken ct = default) =>
        WithDetails()
            .AsNoTracking()
            .VisibleTo(caregiverId)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CareShift?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CareShift?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.CareShifts.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CareShift?> GetByIdWithAssignmentsAsync(int id, CancellationToken ct = default) =>
        _db.CareShifts
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> HasOverlapAsync(int patientId, DateTime start, DateTime end, CancellationToken ct = default) =>
        _db.CareShifts.AnyAsync(s => s.PatientId == patientId && s.StartTime < end && s.EndTime > start, ct);

    public Task<List<Tag>> GetTagsByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default) =>
        _db.Tags.Where(t => names.Contains(t.Name)).ToListAsync(ct);

    public Task<ShiftAssignment?> GetAssignmentAsync(int shiftId, int caregiverId, CancellationToken ct = default) =>
        _db.ShiftAssignments.FirstOrDefaultAsync(a => a.CareShiftId == shiftId && a.CaregiverId == caregiverId, ct);

    public Task<bool> IsAssignedAsync(int shiftId, int caregiverId, CancellationToken ct = default) =>
        _db.ShiftAssignments.AnyAsync(a => a.CareShiftId == shiftId && a.CaregiverId == caregiverId, ct);

    public void Add(CareShift shift) => _db.CareShifts.Add(shift);

    public void AddTag(Tag tag) => _db.Tags.Add(tag);
}
