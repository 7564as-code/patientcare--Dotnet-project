using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;

namespace PatientCare.Infrastructure.Data;

/// <summary>
/// נתוני דמו לפיתוח: מנהלת מערכת, שני בני משפחה, חולה, ומשמרות.
/// הסיסמה של משתמשי הדמו לא נשמרת בקוד אלא מגיעה מ-User Secrets (Seed:DemoPassword).
/// ההרצה בטוחה לחזרה: מה שכבר קיים לא נוצר שוב.
/// </summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@patientcare.local";
    public const string DemoUser1Email = "rachel@patientcare.local";
    public const string DemoUser2Email = "david@patientcare.local";

    public static async Task SeedAsync(
        AppDbContext db,
        string? demoPassword,
        string? promoteToAdminEmail,
        ILogger logger,
        Func<string, string> hashPassword,
        CancellationToken ct = default)
    {
        // קידום משתמש קיים למנהל מערכת (למשל החשבון שכבר נרשם אצלך).
        if (!string.IsNullOrWhiteSpace(promoteToAdminEmail))
        {
            var email = promoteToAdminEmail.Trim().ToLowerInvariant();
            var existing = await db.Caregivers.FirstOrDefaultAsync(c => c.Email == email, ct);
            if (existing is not null && existing.Role != CaregiverRole.Admin)
            {
                existing.Role = CaregiverRole.Admin;
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Caregiver {CaregiverId} promoted to Admin by configuration.", existing.Id);
            }
        }

        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogInformation("Seed:DemoPassword is not configured; demo data was not created.");
            return;
        }

        var hash = hashPassword(demoPassword);

        var admin = await EnsureCaregiverAsync(db, AdminEmail, "מנהלת מערכת", Gender.Female, new DateTime(1980, 1, 1), CaregiverRole.Admin, hash, ct);
        var rachel = await EnsureCaregiverAsync(db, DemoUser1Email, "רחל כהן", Gender.Female, new DateTime(1985, 5, 12), CaregiverRole.User, hash, ct);
        var david = await EnsureCaregiverAsync(db, DemoUser2Email, "דוד לוי", Gender.Male, new DateTime(1982, 9, 3), CaregiverRole.User, hash, ct);

        var patient = await db.Patients
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.ManagerCaregiverId == rachel.Id && p.MedicalCode == "סבתא רחל (דמו)", ct);

        if (patient is null)
        {
            patient = new Patient
            {
                MedicalCode = "סבתא רחל (דמו)",
                PatientGender = Gender.Female,
                DateOfBirth = new DateTime(1942, 3, 20),
                Department = "בית",
                ManagerCaregiverId = rachel.Id,
                InviteCode = "DEMO2345"
            };
            patient.Members.Add(new PatientMember { CaregiverId = david.Id });
            db.Patients.Add(patient);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo patient {PatientId}.", patient.Id);
        }

        var hasShifts = await db.CareShifts.AnyAsync(s => s.PatientId == patient.Id, ct);
        if (!hasShifts)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            db.CareShifts.AddRange(
                new CareShift
                {
                    PatientId = patient.Id,
                    StartTime = tomorrow.AddHours(8),
                    EndTime = tomorrow.AddHours(14),
                    RequiredGender = GenderRequirement.Any,
                    Capacity = 2
                },
                new CareShift
                {
                    PatientId = patient.Id,
                    StartTime = tomorrow.AddHours(14),
                    EndTime = tomorrow.AddHours(20),
                    RequiredGender = GenderRequirement.Female,
                    Capacity = 1
                });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo shifts for patient {PatientId}.", patient.Id);
        }

        _ = admin;
    }

    private static async Task<Caregiver> EnsureCaregiverAsync(
        AppDbContext db,
        string email,
        string fullName,
        Gender gender,
        DateTime birth,
        CaregiverRole role,
        string passwordHash,
        CancellationToken ct)
    {
        var caregiver = await db.Caregivers.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (caregiver is not null)
        {
            return caregiver;
        }

        caregiver = new Caregiver
        {
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            CaregiverGender = gender,
            DateOfBirth = birth,
            Role = role
        };
        db.Caregivers.Add(caregiver);
        await db.SaveChangesAsync(ct);
        return caregiver;
    }
}
