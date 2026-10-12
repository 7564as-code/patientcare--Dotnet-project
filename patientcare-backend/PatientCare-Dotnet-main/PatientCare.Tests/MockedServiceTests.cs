using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Core.Interfaces;
using PatientCare.Service.Services;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

/// <summary>בדיקות יחידה לשירותים עם Mock לשכבת ה-Repositories (בלי בסיס נתונים בכלל).</summary>
public class MockedServiceTests
{
    private static Caregiver NewCaregiver(int id, string name, Gender gender = Gender.Female) => new()
    {
        Id = id,
        FullName = name,
        Email = $"{name}@test.local",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret1", 4),
        CaregiverGender = gender,
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    // ---------- AuthService ----------

    private static (AuthService Sut, Mock<ICaregiverRepository> Repo, Mock<IUnitOfWork> Uow) CreateAuth()
    {
        var repo = new Mock<ICaregiverRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new AuthService(repo.Object, uow.Object, ServiceFactory.TestJwtOptions(), NullLogger<AuthService>.Instance);
        return (sut, repo, uow);
    }

    [Fact]
    public async Task Register_Rejected_WhenEmailAlreadyExists_AndNothingIsSaved()
    {
        var (sut, repo, uow) = CreateAuth();
        repo.Setup(r => r.EmailExistsAsync("a@test.local", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await sut.RegisterAsync(new RegisterRequest("אליס", "a@test.local", "secret1", Gender.Female, new DateTime(1990, 1, 1)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        repo.Verify(r => r.Add(It.IsAny<Caregiver>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_Succeeds_WithUserRole_AndSavesOnce()
    {
        var (sut, repo, uow) = CreateAuth();
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await sut.RegisterAsync(new RegisterRequest("אליס", "a@test.local", "secret1", Gender.Female, new DateTime(1990, 1, 1)));

        Assert.True(result.Succeeded);
        Assert.Equal("User", result.Value!.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        repo.Verify(r => r.Add(It.Is<Caregiver>(c => c.Role == CaregiverRole.User && c.PasswordHash != "secret1")), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var (sut, repo, _) = CreateAuth();
        repo.Setup(r => r.GetByEmailAsync("a@test.local", It.IsAny<CancellationToken>())).ReturnsAsync(NewCaregiver(1, "alice"));

        var bad = await sut.LoginAsync(new LoginRequest("a@test.local", "wrong"));
        var good = await sut.LoginAsync(new LoginRequest("a@test.local", "secret1"));

        Assert.Equal(ServiceErrorType.Unauthorized, bad.ErrorType);
        Assert.True(good.Succeeded);
    }

    // ---------- SwapRequestService ----------

    private static (SwapRequestService Sut, Mock<ISwapRequestRepository> Swaps, Mock<IUnitOfWork> Uow) CreateSwap()
    {
        var swaps = new Mock<ISwapRequestRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new SwapRequestService(
            swaps.Object,
            new Mock<ICareShiftRepository>().Object,
            new Mock<IPatientRepository>().Object,
            new Mock<ICaregiverRepository>().Object,
            uow.Object,
            ServiceFactory.CreateMapper(),
            NullLogger<SwapRequestService>.Instance);
        return (sut, swaps, uow);
    }

    private static ShiftSwapRequest PendingRequest() => new()
    {
        Id = 5,
        RequestorId = 1,
        Requestor = NewCaregiver(1, "alice"),
        TargetCaregiverId = 2,
        TargetCaregiver = NewCaregiver(2, "bob", Gender.Male),
        CareShiftId = 10,
        TargetCareShiftId = 11,
        Status = SwapRequestStatus.Pending
    };

    [Fact]
    public async Task Reject_ByNonTarget_IsForbidden_AndNothingIsSaved()
    {
        var (sut, swaps, uow) = CreateSwap();
        swaps.Setup(r => r.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(PendingRequest());

        var result = await sut.RejectSwapRequestAsync(5, rejecterCaregiverId: 99, reason: "x");

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reject_ByTarget_SavesOnce()
    {
        var (sut, swaps, uow) = CreateSwap();
        var request = PendingRequest();
        swaps.Setup(r => r.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var result = await sut.RejectSwapRequestAsync(5, 2, "לא יכול");

        Assert.True(result.Succeeded);
        Assert.Equal(SwapRequestStatus.Rejected, request.Status);
        Assert.Equal("לא יכול", request.ResponseReason);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenSaveHitsConcurrency_ReturnsConflict()
    {
        var (sut, swaps, uow) = CreateSwap();
        swaps.Setup(r => r.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(PendingRequest());
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PatientCare.Core.Exceptions.ConcurrencyException("race"));

        var result = await sut.RejectSwapRequestAsync(5, 2, null);

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_UnknownRequest_ReturnsNotFound()
    {
        var (sut, swaps, _) = CreateSwap();
        swaps.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftSwapRequest?)null);

        var result = await sut.CancelSwapRequestAsync(123, 1);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }
}
