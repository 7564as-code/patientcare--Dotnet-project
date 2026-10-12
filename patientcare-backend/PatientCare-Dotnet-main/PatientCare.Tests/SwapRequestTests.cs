using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>בקשות החלפה מקצה לקצה על InMemory: יצירה עם בדיקת מין, אישור, דחייה עם סיבה וביטול.</summary>
public class SwapRequestTests
{
    private record Seed(int AliceId, int BobId, int AliceShiftId, int BobShiftId);

    private static Caregiver NewCaregiver(string name, Gender gender) => new()
    {
        FullName = name,
        Email = $"{name}@test.local",
        PasswordHash = "hash",
        CaregiverGender = gender,
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    private static async Task<Seed> SeedAsync(
        string dbName,
        GenderRequirement aliceShiftGender = GenderRequirement.Any,
        GenderRequirement bobShiftGender = GenderRequirement.Any)
    {
        await using var context = InMemoryDbContextFactory.Create(dbName);

        var alice = NewCaregiver("alice", Gender.Female);
        var bob = NewCaregiver("bob", Gender.Male);
        context.Caregivers.AddRange(alice, bob);
        await context.SaveChangesAsync();

        var patient = new Patient
        {
            MedicalCode = "אמא",
            PatientGender = Gender.Female,
            DateOfBirth = new DateTime(1950, 1, 1),
            Department = "בית",
            ManagerCaregiverId = alice.Id
        };
        patient.Members.Add(new PatientMember { CaregiverId = bob.Id });

        var aliceShift = new CareShift
        {
            Patient = patient,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(4),
            RequiredGender = aliceShiftGender,
            Capacity = 1,
            CurrentCount = 1
        };
        aliceShift.Assignments.Add(new ShiftAssignment { CaregiverId = alice.Id });

        var bobShift = new CareShift
        {
            Patient = patient,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(4),
            RequiredGender = bobShiftGender,
            Capacity = 1,
            CurrentCount = 1
        };
        bobShift.Assignments.Add(new ShiftAssignment { CaregiverId = bob.Id });

        context.CareShifts.AddRange(aliceShift, bobShift);
        await context.SaveChangesAsync();

        return new Seed(alice.Id, bob.Id, aliceShift.Id, bobShift.Id);
    }

    [Fact]
    public async Task Create_Succeeds_WhenGendersFit()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);

        var result = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, "תודה"));

        Assert.True(result.Succeeded);
        Assert.Equal(SwapRequestStatus.Pending, result.Value!.Status);
        Assert.Equal("alice", result.Value.RequestorName);
        Assert.Equal("bob", result.Value.TargetCaregiverName);
    }

    [Fact]
    public async Task Create_Rejected_WhenRequestorGenderDoesNotFitTargetShift()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db, bobShiftGender: GenderRequirement.Male);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);

        var result = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("גברים בלבד", result.Error);
    }

    [Fact]
    public async Task Create_Rejected_WhenTargetGenderDoesNotFitMyShift()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db, aliceShiftGender: GenderRequirement.Female);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);

        var result = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("נשים בלבד", result.Error);
    }

    [Fact]
    public async Task Approve_SwapsAssignments_AndOnlyTargetMayApprove()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);
        var created = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));

        var byRequestor = await sut.ApproveSwapRequestAsync(created.Value!.Id, seed.AliceId);
        Assert.Equal(ServiceErrorType.Forbidden, byRequestor.ErrorType);

        var approved = await sut.ApproveSwapRequestAsync(created.Value.Id, seed.BobId);
        Assert.True(approved.Succeeded);
        Assert.Equal(SwapRequestStatus.Approved, approved.Value!.Status);

        await using var verify = InMemoryDbContextFactory.Create(db);
        var shifts = ServiceFactory.Shifts(verify);
        var aliceShift = (await shifts.GetShiftByIdAsync(seed.AliceShiftId, seed.AliceId)).Value!;
        Assert.Equal(seed.BobId, Assert.Single(aliceShift.Caregivers).CaregiverId);
    }

    [Fact]
    public async Task Reject_StoresReason_OnlyTargetMay_AndOnlyOnce()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);
        var created = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));
        var id = created.Value!.Id;

        Assert.Equal(ServiceErrorType.Forbidden, (await sut.RejectSwapRequestAsync(id, seed.AliceId, "x")).ErrorType);

        var rejected = await sut.RejectSwapRequestAsync(id, seed.BobId, "  לא מתאים לי  ");
        Assert.True(rejected.Succeeded);
        Assert.Equal(SwapRequestStatus.Rejected, rejected.Value!.Status);
        Assert.Equal("לא מתאים לי", rejected.Value.ResponseReason);

        Assert.Equal(ServiceErrorType.Validation, (await sut.RejectSwapRequestAsync(id, seed.BobId, null)).ErrorType);
    }

    [Fact]
    public async Task Reject_WithoutReason_IsAllowed()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);
        var created = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));

        var rejected = await sut.RejectSwapRequestAsync(created.Value!.Id, seed.BobId, null);

        Assert.True(rejected.Succeeded);
        Assert.Null(rejected.Value!.ResponseReason);
    }

    [Fact]
    public async Task Cancel_OnlyRequestorMay_AndOnlyWhilePending()
    {
        var db = Guid.NewGuid().ToString();
        var seed = await SeedAsync(db);
        await using var context = InMemoryDbContextFactory.Create(db);
        var sut = ServiceFactory.Swaps(context);
        var created = await sut.CreateSwapRequestAsync(seed.AliceId, new CreateSwapRequestDto(seed.AliceShiftId, seed.BobShiftId, null));
        var id = created.Value!.Id;

        Assert.Equal(ServiceErrorType.Forbidden, (await sut.CancelSwapRequestAsync(id, seed.BobId)).ErrorType);

        var cancelled = await sut.CancelSwapRequestAsync(id, seed.AliceId);
        Assert.True(cancelled.Succeeded);
        Assert.Equal(SwapRequestStatus.Cancelled, cancelled.Value!.Status);

        Assert.Equal(ServiceErrorType.Validation, (await sut.CancelSwapRequestAsync(id, seed.AliceId)).ErrorType);
    }
}
