using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Exceptions;
using PatientCare.Core.Interfaces;

namespace PatientCare.Infrastructure.Data;

/// <summary>
/// שומר את כל השינויים של ה-DbContext בטרנזקציה אחת, וממיר חריגות של EF Core לחריגות של Core,
/// כדי ששכבת השירות לא תהיה תלויה ב-EF Core.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("השורה שונתה על ידי משתמש אחר בין הקריאה לשמירה.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new DataConstraintException("השמירה נכשלה בגלל הפרת אילוץ בבסיס הנתונים.", ex);
        }
    }
}
