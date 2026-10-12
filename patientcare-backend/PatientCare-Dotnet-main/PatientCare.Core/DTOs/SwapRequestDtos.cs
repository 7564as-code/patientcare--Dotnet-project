using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

public record CreateSwapRequestDto(
    [property: Range(1, int.MaxValue, ErrorMessage = "יש לבחור את המשמרת שלך.")]
    int CareShiftId,
    [property: Range(1, int.MaxValue, ErrorMessage = "יש לבחור משמרת יעד.")]
    int TargetCareShiftId,
    [property: StringLength(500, ErrorMessage = "הסיבה יכולה להכיל עד 500 תווים.")]
    string? Reason);

/// <summary>דחיית בקשת החלפה. הסיבה אופציונלית.</summary>
public record RejectSwapRequestDto(
    [property: StringLength(500, ErrorMessage = "הסיבה יכולה להכיל עד 500 תווים.")]
    string? Reason);

public record SwapRequestResponse(
    int Id,
    int RequestorId,
    string RequestorName,
    int? TargetCaregiverId,
    string? TargetCaregiverName,
    int CareShiftId,
    SwapRequestStatus Status,
    string? Reason,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    int TargetCareShiftId,
    string? ResponseReason = null);
