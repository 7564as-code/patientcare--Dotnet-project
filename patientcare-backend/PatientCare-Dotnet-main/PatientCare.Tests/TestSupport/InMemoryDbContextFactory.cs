using Microsoft.EntityFrameworkCore;
using PatientCare.Infrastructure.Data;

namespace PatientCare.Tests.TestSupport;

/// <summary>
/// Creates AppDbContext instances backed by EF Core's InMemory provider.
/// The provider honours [Timestamp]/IsRowVersion concurrency tokens the same way a real
/// relational provider does, so it is used here to exercise optimistic-concurrency behaviour
/// without requiring a live SQL Server instance for the test suite to run.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>Creates a context on a fresh, uniquely named database (isolated from other tests).</summary>
    public static AppDbContext CreateNew() => Create(Guid.NewGuid().ToString());

    /// <summary>Creates a context on a named database, so multiple contexts can share the same store.</summary>
    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ConcurrencyAwareTestDbContext(options);
    }
}
