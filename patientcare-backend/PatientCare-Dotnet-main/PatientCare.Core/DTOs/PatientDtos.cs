using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

/// <param name="MedicalCode">שם או כינוי של החולה (נשאר בשם הישן לתאימות).</param>
/// <param name="Department">מקום הטיפול (נשאר בשם הישן לתאימות).</param>
public record CreatePatientRequest(
    [Required(ErrorMessage = "יש להזין שם לחולה."),
               StringLength(50, MinimumLength = 1, ErrorMessage = "שם החולה יכול להכיל עד 50 תווים.")]
    string MedicalCode,
    [EnumDataType(typeof(Gender), ErrorMessage = "המין שנבחר אינו תקין.")]
    Gender PatientGender,
    DateTime DateOfBirth,
    [Required(ErrorMessage = "יש להזין מקום טיפול."),
               StringLength(100, MinimumLength = 1, ErrorMessage = "מקום הטיפול יכול להכיל עד 100 תווים.")]
    string Department);

public record PatientResponse(
    int Id,
    string MedicalCode,
    Gender PatientGender,
    DateTime DateOfBirth,
    string Department,
    int Age,
    AgeCategory AgeCategory,
    int? ManagerCaregiverId,
    bool IsManager);

public record ChangePatientManagerRequest(
    [Range(1, int.MaxValue, ErrorMessage = "יש לבחור מנהל חדש.")]
    int NewManagerCaregiverId);

public record PatientMemberResponse(
    int CaregiverId,
    string FullName,
    bool IsManager);

public record InviteCodeResponse(string InviteCode);

public record JoinPatientRequest(
    [Required(ErrorMessage = "יש להזין קוד הזמנה."),
               StringLength(16, ErrorMessage = "קוד ההזמנה ארוך מדי.")]
    string InviteCode);
