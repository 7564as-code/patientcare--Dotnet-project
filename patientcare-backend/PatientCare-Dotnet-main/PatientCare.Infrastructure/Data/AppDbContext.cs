using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Entities;

namespace PatientCare.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Caregiver> Caregivers => Set<Caregiver>();
    public DbSet<CareShift> CareShifts => Set<CareShift>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PatientMember> PatientMembers => Set<PatientMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// בכל הוספה או עדכון של משמרת נקבע RowVersion חדש. EF משווה את הערך המקורי בתנאי ה-WHERE של העדכון,
    /// ולכן מי ששמר שני ויש בידיו ערך ישן נכשל ב-DbUpdateConcurrencyException.
    /// </summary>
    private void StampRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<CareShift>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(s => s.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
