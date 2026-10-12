using PatientCare.Core.Common;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Service.Services;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>
/// Service-layer tests for the contested resource in this project: a CareShift's remaining
/// capacity. Each test seeds an isolated InMemory database, so tests never interfere with
/// each other.
/// </summary>
public class CareShiftServiceTests
{
    private static async Task<(int ShiftId, int CaregiverId)> SeedShiftWithCaregiverAsync(
        string dbName,
        int capacity,
        int currentCount,
        GenderRequirement requiredGender = GenderRequirement.Any,
        Gender caregiverGender = Gender.Female)
    {
        await using var context = InMemoryDbContextFactory.Create(dbName);

        var patient = new Patient
        {
            MedicalCode = "MC-1",
            PatientGender = Gender.Female,
            DateOfBirth = new DateTime(1990, 1, 1),
            Department = "General"
        };
        var caregiver = new Caregiver
        {
            FullName = "Test Caregiver",
            Email = "caregiver@test.local",
            PasswordHash = "hash",
            CaregiverGender = caregiverGender,
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var shift = new CareShift
        {
            Patient = patient,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(8),
            RequiredGender = requiredGender,
            Capacity = capacity,
            CurrentCount = currentCount
        };

        // המעגל של החולה: רק חבר במעגל יכול להשתבץ.
        patient.Members.Add(new PatientMember { Caregiver = caregiver });

        context.Patients.Add(patient);
        context.Caregivers.Add(caregiver);
        context.CareShifts.Add(shift);
        await context.SaveChangesAsync();

        return (shift.Id, caregiver.Id);
    }

    [Fact]
    public async Task RegisterCaregiverForShiftAsync_Succeeds_WhenCapacityIsAvailable()
    {
        var dbName = Guid.NewGuid().ToString();
        var (shiftId, caregiverId) = await SeedShiftWithCaregiverAsync(dbName, capacity: 2, currentCount: 0);

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = ServiceFactory.Shifts(context);

        var result = await sut.RegisterCaregiverForShiftAsync(shiftId, caregiverId);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.CurrentCount);
        Assert.False(result.Value.IsFull);
    }

    [Fact]
    public async Task RegisterCaregiverForShiftAsync_Rejected_WhenShiftIsFull()
    {
        var dbName = Guid.NewGuid().ToString();
        var (shiftId, caregiverId) = await SeedShiftWithCaregiverAsync(dbName, capacity: 1, currentCount: 1);

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = ServiceFactory.Shifts(context);

        var result = await sut.RegisterCaregiverForShiftAsync(shiftId, caregiverId);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("מקומות", result.Error);
    }

    [Fact]
    public async Task RegisterCaregiverForShiftAsync_Rejected_WhenCaregiverGenderDoesNotMatch()
    {
        var dbName = Guid.NewGuid().ToString();
        var (shiftId, caregiverId) = await SeedShiftWithCaregiverAsync(
            dbName,
            capacity: 2,
            currentCount: 0,
            requiredGender: GenderRequirement.Male,
            caregiverGender: Gender.Female);

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = ServiceFactory.Shifts(context);

        var result = await sut.RegisterCaregiverForShiftAsync(shiftId, caregiverId);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("מין", result.Error);
    }

    [Fact]
    public async Task RegisterCaregiverForShiftAsync_Rejected_WhenCaregiverAlreadyAssigned()
    {
        var dbName = Guid.NewGuid().ToString();
        var (shiftId, caregiverId) = await SeedShiftWithCaregiverAsync(dbName, capacity: 3, currentCount: 0);

        await using (var setupContext = InMemoryDbContextFactory.Create(dbName))
        {
            var setupSut = ServiceFactory.Shifts(setupContext);
            var first = await setupSut.RegisterCaregiverForShiftAsync(shiftId, caregiverId);
            Assert.True(first.Succeeded);
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = ServiceFactory.Shifts(context);

        var result = await sut.RegisterCaregiverForShiftAsync(shiftId, caregiverId);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("כבר משובץ", result.Error);
    }

    [Fact]
    public async Task RegisterCaregiverForShiftAsync_ReturnsNotFound_WhenShiftDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (_, caregiverId) = await SeedShiftWithCaregiverAsync(dbName, capacity: 1, currentCount: 0);

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = ServiceFactory.Shifts(context);

        var result = await sut.RegisterCaregiverForShiftAsync(shiftId: -1, caregiverId);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }
}
