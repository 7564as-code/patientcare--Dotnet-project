using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>עימוד, חיפוש ומיון מתבצעים בשאילתה מול בסיס הנתונים, והרשאות מנהל מערכת.</summary>
public class PagingAndAdminTests
{
    private static async Task<int> SeedManagerWithPatientsAsync(string dbName, int count)
    {
        await using var context = InMemoryDbContextFactory.Create(dbName);
        var manager = new Caregiver
        {
            FullName = "manager",
            Email = "m@test.local",
            PasswordHash = "hash",
            CaregiverGender = Gender.Female,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        context.Caregivers.Add(manager);
        await context.SaveChangesAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Patients.Add(new Patient
            {
                MedicalCode = $"חולה {i:00}",
                PatientGender = Gender.Female,
                DateOfBirth = new DateTime(1940 + i, 1, 1),
                Department = i % 2 == 0 ? "בית" : "בית חולים",
                ManagerCaregiverId = manager.Id
            });
        }

        await context.SaveChangesAsync();
        return manager.Id;
    }

    [Fact]
    public async Task GetPatients_ReturnsRequestedPageAndTotals()
    {
        var db = Guid.NewGuid().ToString();
        var managerId = await SeedManagerWithPatientsAsync(db, 25);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        var page = await sut.GetPatientsAsync(managerId, new PatientQuery { Page = 3, PageSize = 10 });

        Assert.Equal(5, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(3, page.Page);
    }

    [Fact]
    public async Task GetPatients_FiltersByDepartmentAndSearch()
    {
        var db = Guid.NewGuid().ToString();
        var managerId = await SeedManagerWithPatientsAsync(db, 10);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        var byDepartment = await sut.GetPatientsAsync(managerId, new PatientQuery { Department = "בית" });
        var bySearch = await sut.GetPatientsAsync(managerId, new PatientQuery { Search = "חולה 03" });

        Assert.Equal(5, byDepartment.TotalCount);
        Assert.Equal("חולה 03", Assert.Single(bySearch.Items).MedicalCode);
    }

    [Fact]
    public async Task GetPatients_SortsByAgeAscending_YoungestFirst()
    {
        var db = Guid.NewGuid().ToString();
        var managerId = await SeedManagerWithPatientsAsync(db, 5);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        var page = await sut.GetPatientsAsync(managerId, new PatientQuery { SortBy = PatientSortBy.Age });

        Assert.Equal("חולה 05", page.Items[0].MedicalCode);
        Assert.Equal("חולה 01", page.Items[^1].MedicalCode);
    }

    [Fact]
    public async Task Admin_CanSeeAllPatients_AndChangeRoles_ButNotDemoteSelf()
    {
        var db = Guid.NewGuid().ToString();
        var managerId = await SeedManagerWithPatientsAsync(db, 3);
        await using var context = InMemoryDbContextFactory.Create(db);

        var admin = new Caregiver
        {
            FullName = "admin",
            Email = "admin@test.local",
            PasswordHash = "hash",
            CaregiverGender = Gender.Female,
            DateOfBirth = new DateTime(1980, 1, 1),
            Role = CaregiverRole.Admin
        };
        context.Caregivers.Add(admin);
        await context.SaveChangesAsync();

        var sut = ServiceFactory.Admin(context);

        var allPatients = await sut.GetAllPatientsAsync(new PatientQuery(), admin.Id);
        Assert.Equal(3, allPatients.TotalCount);

        var caregivers = await sut.GetCaregiversAsync(new PageQuery { PageSize = 1 });
        Assert.Equal(2, caregivers.TotalCount);
        Assert.Single(caregivers.Items);

        var promoted = await sut.ChangeRoleAsync(managerId, CaregiverRole.Admin, admin.Id);
        Assert.True(promoted.Succeeded);
        Assert.Equal(CaregiverRole.Admin, promoted.Value!.Role);

        var selfDemote = await sut.ChangeRoleAsync(admin.Id, CaregiverRole.User, admin.Id);
        Assert.Equal(ServiceErrorType.Validation, selfDemote.ErrorType);

        var missing = await sut.ChangeRoleAsync(9999, CaregiverRole.User, admin.Id);
        Assert.Equal(ServiceErrorType.NotFound, missing.ErrorType);
    }
}
