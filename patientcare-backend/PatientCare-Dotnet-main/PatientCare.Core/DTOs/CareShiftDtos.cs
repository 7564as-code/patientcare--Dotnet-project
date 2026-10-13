using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

public record CreateCareShiftRequest(
    [Range(1, int.MaxValue, ErrorMessage = "יש לבחור חולה.")]
    int PatientId,
    DateTime StartTime,
    DateTime EndTime,
    [EnumDataType(typeof(GenderRequirement), ErrorMessage = "דרישת המין שנבחרה אינה תקינה.")]
    GenderRequirement RequiredGender,
    [Range(1, 100, ErrorMessage = "מספר בני המשפחה במשמרת חייב להיות גדול מאפס.")]
    int Capacity,
    [MaxLength(20, ErrorMessage = "אפשר להוסיף עד 20 תגיות.")]
    List<string>? Tags);

/// <summary>בן משפחה שמשובץ למשמרת.</summary>
public record ShiftCaregiverResponse(int CaregiverId, string FullName);

public record CareShiftResponse(
    int Id,
    int PatientId,
    string PatientMedicalCode,
    DateTime StartTime,
    DateTime EndTime,
    GenderRequirement RequiredGender,
    int Capacity,
    int CurrentCount,
    bool IsFull,
    List<string> Tags,
    List<ShiftCaregiverResponse> Caregivers);
