using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Core.Interfaces;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Infrastructure.Repositories;

public class SwapRequestRepository : ISwapRequestRepository
{
    private readonly AppDbContext _db;

    public SwapRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ShiftSwapRequest?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default) =>
        _db.ShiftSwapRequests
            .Include(r => r.CareShift)
            .Include(r => r.TargetCareShift)
            .Include(r => r.Requestor)
            .Include(r => r.TargetCaregiver)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<ShiftSwapRequest>> GetPendingForManagerAsync(int managerCaregiverId, CancellationToken ct = default) =>
        _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.Requestor)
            .Include(r => r.TargetCaregiver)
            .Include(r => r.CareShift)
                .ThenInclude(s => s.Patient)
            .Where(r =>
                r.Status == SwapRequestStatus.Pending &&
                r.CareShift.Patient.ManagerCaregiverId == managerCaregiverId)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(ct);

    public Task<List<ShiftSwapRequest>> GetIncomingAsync(int caregiverId, CancellationToken ct = default) =>
        _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.Requestor)
            .Include(r => r.TargetCaregiver)
            .Where(r => r.TargetCaregiverId == caregiverId && r.Status == SwapRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(ct);

    public Task<List<ShiftSwapRequest>> GetMineAsync(int caregiverId, CancellationToken ct = default) =>
        _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.Requestor)
            .Include(r => r.TargetCaregiver)
            .Where(r => r.RequestorId == caregiverId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    public void Add(ShiftSwapRequest request) => _db.ShiftSwapRequests.Add(request);
}
