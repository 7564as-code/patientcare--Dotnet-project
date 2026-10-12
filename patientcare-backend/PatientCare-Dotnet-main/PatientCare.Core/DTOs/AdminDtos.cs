using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

public record CaregiverSummaryResponse(
    int Id,
    string FullName,
    string Email,
    Gender CaregiverGender,
    DateTime DateOfBirth,
    CaregiverRole Role);

public record ChangeCaregiverRoleRequest(
    [property: EnumDataType(typeof(CaregiverRole), ErrorMessage = "התפקיד שנבחר אינו תקין.")]
    CaregiverRole Role);
