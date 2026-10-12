using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Service.Services;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>כללי הגישה של מערכת המשפחה: מעגל לכל חולה, ורק המנהל יוצר משמרות.</summary>
public class FamilyAccessTests
{
    private record Seed(int PatientId, int ManagerId, int MemberId, int OutsiderId, int ShiftId);

    private static Caregiver NewCaregiver(string name) => new()
    {
        FullName = name,
        Email = $"{name}@test.local",
        PasswordHash = "hash",
        CaregiverGender = Gender.Female,
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    private static async Task<Seed> SeedAsync(string dbName)
    {
        await using var context = InMemoryDbContextFactory.Create(dbName);

        var manager = NewCaregiver("manager");
        var member = NewCaregiver("member");
        var outsider = NewCaregiver("outsider");
        context.Caregivers.AddRange(manager, member, outsider);
        await context.SaveChangesAsync();

        var patient = new Patient
        {
            MedicalCode = "אמא",
            PatientGender = Gender.Female,
            DateOfBirth = new DateTime(1950, 1, 1),
            Department = "בית",
            ManagerCaregiverId = manager.Id,
            InviteCode = "ABCD2345"
        };
        patient.Members.Add(new PatientMember { CaregiverId = member.Id });
        var shift = new CareShift
        {
            Patient = patient,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(8),
            Capacity = 2
        };
        context.CareShifts.Add(shift);
        await context.SaveChangesAsync();

        return new Seed(patient.Id, manager.Id, member.Id, outsider.Id, shift.Id);
    }

    [Fact]
    public async Task Patients_AreVisibleOnlyToManagerAndMembers()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        Assert.Single((await sut.GetPatientsAsync(seed.ManagerId, new PatientQuery())).Items);
        Assert.Single((await sut.GetPatientsAsync(seed.MemberId, new PatientQuery())).Items);
        Assert.Empty((await sut.GetPatientsAsync(seed.OutsiderId, new PatientQuery())).Items);
        Assert.Equal(ServiceErrorType.NotFound, (await sut.GetPatientByIdAsync(seed.PatientId, seed.OutsiderId)).ErrorType);
    }

    [Fact]
    public async Task Shifts_AreVisibleOnlyToMembers()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Shifts(context);

        Assert.Single((await sut.GetShiftsAsync(seed.MemberId, new CareShiftQuery())).Items);
        Assert.Empty((await sut.GetShiftsAsync(seed.OutsiderId, new CareShiftQuery())).Items);
    }

    [Fact]
    public async Task CreateShift_OnlyManagerMaySucceed()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Shifts(context);
        var request = new CreateCareShiftRequest(
            seed.PatientId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(8), GenderRequirement.Any, 1, null);

        var asMember = await sut.CreateShiftAsync(request, seed.MemberId);
        var asOutsider = await sut.CreateShiftAsync(request, seed.OutsiderId);
        var asManager = await sut.CreateShiftAsync(request, seed.ManagerId);

        Assert.Equal(ServiceErrorType.Forbidden, asMember.ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, asOutsider.ErrorType);
        Assert.True(asManager.Succeeded);
    }

    [Fact]
    public async Task Register_RejectedForOutsider_AllowedForMember_AndShowsName()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Shifts(context);

        var outsider = await sut.RegisterCaregiverForShiftAsync(seed.ShiftId, seed.OutsiderId);
        var member = await sut.RegisterCaregiverForShiftAsync(seed.ShiftId, seed.MemberId);

        Assert.Equal(ServiceErrorType.Forbidden, outsider.ErrorType);
        Assert.True(member.Succeeded);
        Assert.Contains(member.Value!.Caregivers, c => c.FullName == "member");
    }

    [Fact]
    public async Task Join_WithInviteCode_AddsMember_AndRejectsBadCodeAndDuplicates()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        Assert.Equal(ServiceErrorType.NotFound, (await sut.JoinAsync("WRONG", seed.OutsiderId)).ErrorType);

        var joined = await sut.JoinAsync(" abcd2345 ", seed.OutsiderId);
        Assert.True(joined.Succeeded);
        Assert.False(joined.Value!.IsManager);
        Assert.Single((await sut.GetPatientsAsync(seed.OutsiderId, new PatientQuery())).Items);

        Assert.Equal(ServiceErrorType.Validation, (await sut.JoinAsync("ABCD2345", seed.OutsiderId)).ErrorType);
        Assert.Equal(ServiceErrorType.Validation, (await sut.JoinAsync("ABCD2345", seed.ManagerId)).ErrorType);
    }

    [Fact]
    public async Task ChangeManager_RequiresMember_AndKeepsOldManagerInCircle()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        var toOutsider = await sut.ChangeManagerAsync(seed.PatientId, seed.ManagerId, seed.OutsiderId);
        Assert.Equal(ServiceErrorType.Validation, toOutsider.ErrorType);

        var notManager = await sut.ChangeManagerAsync(seed.PatientId, seed.MemberId, seed.ManagerId);
        Assert.Equal(ServiceErrorType.Forbidden, notManager.ErrorType);

        var ok = await sut.ChangeManagerAsync(seed.PatientId, seed.ManagerId, seed.MemberId);
        Assert.True(ok.Succeeded);

        Assert.Single((await sut.GetPatientsAsync(seed.ManagerId, new PatientQuery())).Items);
        var members = await sut.GetMembersAsync(seed.PatientId, seed.MemberId);
        Assert.Equal(2, members.Value!.Count);
        Assert.True(members.Value[0].IsManager);
        Assert.Equal(seed.MemberId, members.Value[0].CaregiverId);
    }

    [Fact]
    public async Task InviteCode_IsManagerOnly()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Patients(context);

        Assert.Equal(ServiceErrorType.Forbidden, (await sut.GetInviteCodeAsync(seed.PatientId, seed.MemberId, false)).ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, (await sut.GetInviteCodeAsync(seed.PatientId, seed.OutsiderId, false)).ErrorType);

        var same = await sut.GetInviteCodeAsync(seed.PatientId, seed.ManagerId, false);
        Assert.Equal("ABCD2345", same.Value!.InviteCode);

        var reset = await sut.GetInviteCodeAsync(seed.PatientId, seed.ManagerId, true);
        Assert.NotEqual("ABCD2345", reset.Value!.InviteCode);
    }
}
