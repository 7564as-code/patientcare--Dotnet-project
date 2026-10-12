using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Entities;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Tests.TestSupport;

/// <summary>
/// EF Core's InMemory provider does not auto-regenerate a [Timestamp]/rowversion value on
/// update the way SQL Server does, so on its own it can never detect a concurrency conflict on
/// CareShift.RowVersion. This test-only subclass reproduces that store behaviour (assign a new
/// token on every modification) so DbUpdateConcurrencyException is still thrown exactly as it
/// would be against a real SQL Server database.
/// </summary>
public class ConcurrencyAwareTestDbContext : AppDbContext
{
    public ConcurrencyAwareTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<CareShift>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property(s => s.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
