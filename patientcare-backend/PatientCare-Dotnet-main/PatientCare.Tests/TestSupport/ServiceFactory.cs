using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientCare.Core.Common;
using PatientCare.Infrastructure.Data;
using PatientCare.Infrastructure.Repositories;
using PatientCare.Service.Mapping;
using PatientCare.Service.Services;

namespace PatientCare.Tests.TestSupport;

/// <summary>בונה שירותים אמיתיים (repositories + Unit of Work + AutoMapper) מעל DbContext של InMemory.</summary>
public static class ServiceFactory
{
    public static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    public static PatientService Patients(AppDbContext db) =>
        new(new PatientRepository(db), new UnitOfWork(db), CreateMapper(), NullLogger<PatientService>.Instance);

    public static CareShiftService Shifts(AppDbContext db) =>
        new(new CareShiftRepository(db), new PatientRepository(db), new CaregiverRepository(db),
            new UnitOfWork(db), CreateMapper(), NullLogger<CareShiftService>.Instance);

    public static SwapRequestService Swaps(AppDbContext db) =>
        new(new SwapRequestRepository(db), new CareShiftRepository(db), new PatientRepository(db),
            new CaregiverRepository(db), new UnitOfWork(db), CreateMapper(), NullLogger<SwapRequestService>.Instance);

    public static AdminService Admin(AppDbContext db) =>
        new(new CaregiverRepository(db), new PatientRepository(db), new UnitOfWork(db),
            CreateMapper(), NullLogger<AdminService>.Instance);

    public static AuthService Auth(AppDbContext db) =>
        new(new CaregiverRepository(db), new UnitOfWork(db), TestJwtOptions(), NullLogger<AuthService>.Instance);

    public static IOptions<JwtSettings> TestJwtOptions() => Options.Create(new JwtSettings
    {
        Key = "unit-test-secret-key-that-is-long-enough-123456",
        Issuer = "tests",
        Audience = "tests",
        ExpiryMinutes = 5
    });
}
