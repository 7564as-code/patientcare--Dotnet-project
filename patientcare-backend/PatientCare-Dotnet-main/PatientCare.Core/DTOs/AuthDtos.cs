using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

public record RegisterRequest(
    [Required(ErrorMessage = "יש להזין שם מלא."),
               StringLength(150, MinimumLength = 2, ErrorMessage = "השם חייב להכיל בין 2 ל-150 תווים.")]
    string FullName,
    [Required(ErrorMessage = "יש להזין כתובת אימייל."),
               EmailAddress(ErrorMessage = "כתובת האימייל אינה תקינה."),
               StringLength(256, ErrorMessage = "כתובת האימייל ארוכה מדי.")]
    string Email,
    [Required(ErrorMessage = "יש להזין סיסמה."),
               StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים.")]
    string Password,
    [EnumDataType(typeof(Gender), ErrorMessage = "המין שנבחר אינו תקין.")]
    Gender CaregiverGender,
    DateTime DateOfBirth
    );

public record LoginRequest(
    [Required(ErrorMessage = "יש להזין כתובת אימייל.")]
    string Email,
    [Required(ErrorMessage = "יש להזין סיסמה.")]
    string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, string FullName, string Role = "User");
