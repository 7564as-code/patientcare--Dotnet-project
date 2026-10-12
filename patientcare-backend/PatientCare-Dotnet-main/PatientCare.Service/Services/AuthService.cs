using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Exceptions;
using PatientCare.Core.Interfaces;

namespace PatientCare.Service.Services;

public class AuthService : IAuthService
{
    private readonly ICaregiverRepository _caregivers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ICaregiverRepository caregivers,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _caregivers = caregivers;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.DateOfBirth.Date > DateTime.UtcNow.Date)
        {
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Validation, "תאריך הלידה לא יכול להיות בעתיד.");
        }

        // אימייל נשמר באותיות קטנות, כי PostgreSQL רגיש לאותיות גדולות/קטנות.
        var email = NormalizeEmail(request.Email);

        if (await _caregivers.EmailExistsAsync(email, ct))
        {
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Validation, "כתובת האימייל כבר רשומה.");
        }

        var caregiver = new Caregiver
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CaregiverGender = request.CaregiverGender,
            DateOfBirth = request.DateOfBirth
            // Role נשאר User כברירת מחדל. רק מנהל מערכת יכול לשנות תפקיד.
        };

        _caregivers.Add(caregiver);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DataConstraintException)
        {
            // שתי הרשמות במקביל עם אותו אימייל (האינדקס הייחודי).
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Validation, "כתובת האימייל כבר רשומה.");
        }

        _logger.LogInformation("Caregiver {CaregiverId} registered.", caregiver.Id);
        return ServiceResult<AuthResponse>.Success(GenerateToken(caregiver));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var caregiver = await _caregivers.GetByEmailAsync(NormalizeEmail(request.Email), ct);
        if (caregiver is null || !BCrypt.Net.BCrypt.Verify(request.Password, caregiver.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt.");
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Unauthorized, "אימייל או סיסמה שגויים.");
        }

        _logger.LogInformation("Caregiver {CaregiverId} logged in.", caregiver.Id);
        return ServiceResult<AuthResponse>.Success(GenerateToken(caregiver));
    }

    private static string NormalizeEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private AuthResponse GenerateToken(Caregiver caregiver)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, caregiver.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, caregiver.Email),
            new(ClaimTypes.Name, caregiver.FullName),
            new("gender", caregiver.CaregiverGender.ToString()),
            new(ClaimTypes.Role, caregiver.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(tokenString, expiresAt, caregiver.FullName, caregiver.Role.ToString());
    }
}
