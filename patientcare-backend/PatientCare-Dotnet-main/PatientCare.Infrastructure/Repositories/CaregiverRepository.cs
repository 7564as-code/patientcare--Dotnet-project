using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Interfaces;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Infrastructure.Repositories;

public class CaregiverRepository : ICaregiverRepository
{
    private readonly AppDbContext _db;

    public CaregiverRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Caregiver?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Caregivers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Caregiver?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Caregivers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        _db.Caregivers.AnyAsync(c => c.Email == email, ct);

    public async Task<PagedResult<Caregiver>> GetPagedAsync(PageQuery query, CancellationToken ct = default)
    {
        IQueryable<Caregiver> source = _db.Caregivers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            source = source.Where(c => c.FullName.ToLower().Contains(term) || c.Email.ToLower().Contains(term));
        }

        var total = await source.CountAsync(ct);

        var ordered = query.Descending
            ? source.OrderByDescending(c => c.FullName)
            : source.OrderBy(c => c.FullName);

        var items = await ordered
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Caregiver>(items, query.Page, query.PageSize, total);
    }

    public void Add(Caregiver caregiver) => _db.Caregivers.Add(caregiver);
}
