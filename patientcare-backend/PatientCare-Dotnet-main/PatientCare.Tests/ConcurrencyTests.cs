using Microsoft.EntityFrameworkCore;
using PatientCare.Core.Common;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Service.Services;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>
/// Proves the optimistic-concurrency requirement: when two callers race for the same
/// CareShift slot, the first SaveChanges wins and the second is rejected via the
/// RowVersion concurrency token, never silently overbooking the shift.
/// </summary>
public class ConcurrencyTests
{
    private static async Task<int> SeedShiftAsync(string dbName, int capacity = 1, int currentCount = 0)
    {
        await using var context = InMemoryDbContextFactory.Create(dbName);

        var patient = new Patient
        {
            MedicalCode = "MC-1",
            PatientGender = Gender.Female,
            DateOfBirth = new DateTime(1990, 1, 1),
            Department = "General"
        };
        var shift = new CareShift
        {
            Patient = patient,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(8),
            RequiredGender = GenderRequirement.Any,
            Capacity = capacity,
            CurrentCount = currentCount
        };

        context.Patients.Add(patient);
        context.CareShifts.Add(shift);
        await context.SaveChangesAsync();

        return shift.Id;
    }

    /// <summary>
    /// The exact scenario the assignment describes: two separate DbContext instances read the
    /// same row, both modify it, the first SaveChanges succeeds and the second throws
    /// DbUpdateConcurrencyException because its RowVersion is now stale.
    /// </summary>
    [Fact]
    public async Task TwoContextsRaceOnSameRow_FirstSaveSucceeds_SecondSaveThrowsConcurrencyException()
    {
        var dbName = Guid.NewGuid().ToString();
        var shiftId = await SeedShiftAsync(dbName);

        await using var context1 = InMemoryDbContextFactory.Create(dbName);
        await using var context2 = InMemoryDbContextFactory.Create(dbName);

        var shiftInContext1 = await context1.CareShifts.FirstAsync(s => s.Id == shiftId);
        var shiftInContext2 = await context2.CareShifts.FirstAsync(s => s.Id == shiftId);

        shiftInContext1.CurrentCount++;
        shiftInContext2.CurrentCount++;

        await context1.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
    }

    /// <summary>
    /// Same race, but driven through CareShiftService.RegisterCaregiverForShiftAsync end to
    /// end, proving the service catches DbUpdateConcurrencyException and reports it as a
    /// Conflict (mapped to HTTP 409 by the controller) instead of an unhandled exception.
    /// </summary>
    [Fact]
    public async Task RegisterCaregiverForShiftAsync_LastSlotRace_LoserGetsConflictResult()
    {
        var dbName = Guid.NewGuid().ToString();
        var shiftId = await SeedShiftAsync(dbName, capacity: 1, currentCount: 0);

        await using var context1 = InMemoryDbContextFactory.Create(dbName);
        await using var context2 = InMemoryDbContextFactory.Create(dbName);

        var caregiverA = new Caregiver
        {
            FullName = "Caregiver A",
            Email = "a@test.local",
            PasswordHash = "hash",
            CaregiverGender = Gender.Female,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var caregiverB = new Caregiver
        {
            FullName = "Caregiver B",
            Email = "b@test.local",
            PasswordHash = "hash",
            CaregiverGender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        context1.Caregivers.Add(caregiverA);
        context2.Caregivers.Add(caregiverB);
        await context1.SaveChangesAsync();
        await context2.SaveChangesAsync();

        // שני בני המשפחה חברים במעגל של החולה.
        var patientId = (await context1.CareShifts.AsNoTracking().FirstAsync(s => s.Id == shiftId)).PatientId;
        context1.PatientMembers.Add(new PatientMember { PatientId = patientId, CaregiverId = caregiverA.Id });
        context2.PatientMembers.Add(new PatientMember { PatientId = patientId, CaregiverId = caregiverB.Id });
        await context1.SaveChangesAsync();
        await context2.SaveChangesAsync();

        // Both contexts read the shift (and therefore its RowVersion) before either one commits.
        await context1.CareShifts.FirstAsync(s => s.Id == shiftId);
        await context2.CareShifts.FirstAsync(s => s.Id == shiftId);

        var service1 = ServiceFactory.Shifts(context1);
        var service2 = ServiceFactory.Shifts(context2);

        var winner = await service1.RegisterCaregiverForShiftAsync(shiftId, caregiverA.Id);
        var loser = await service2.RegisterCaregiverForShiftAsync(shiftId, caregiverB.Id);

        Assert.True(winner.Succeeded);
        Assert.True(winner.Value!.IsFull);

        Assert.False(loser.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, loser.ErrorType);

        await using var verifyContext = InMemoryDbContextFactory.Create(dbName);
        var finalShift = await verifyContext.CareShifts.AsNoTracking().FirstAsync(s => s.Id == shiftId);
        Assert.Equal(1, finalShift.CurrentCount);
    }
}
