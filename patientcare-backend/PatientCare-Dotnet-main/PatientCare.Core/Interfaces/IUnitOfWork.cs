namespace PatientCare.Core.Interfaces;

/// <summary>
/// שומר את כל השינויים שנעשו דרך ה-repositories בטרנזקציה אחת.
/// מתנגשות אופטימיות מגיעות כ-<see cref="Exceptions.ConcurrencyException"/>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
