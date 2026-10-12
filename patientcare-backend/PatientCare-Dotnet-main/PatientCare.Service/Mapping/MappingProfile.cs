using AutoMapper;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;

namespace PatientCare.Service.Mapping;

/// <summary>המרות בין ישויות ל-DTOs. כל ההמרות במקום אחד, בלי new ידני בכל שירות.</summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientResponse>()
            .ForCtorParam(nameof(PatientResponse.Age), o => o.MapFrom(s => s.GetAge()))
            .ForCtorParam(nameof(PatientResponse.AgeCategory), o => o.MapFrom(s => s.GetAgeCategory()))
            // IsManager תלוי במי שצופה, ולכן נקבע אחרי ההמרה (ראו PatientResponseExtensions.ForViewer).
            .ForCtorParam(nameof(PatientResponse.IsManager), o => o.MapFrom(s => false));

        CreateMap<ShiftAssignment, ShiftCaregiverResponse>()
            .ForCtorParam(nameof(ShiftCaregiverResponse.FullName),
                o => o.MapFrom(s => s.Caregiver != null ? s.Caregiver.FullName : string.Empty));

        CreateMap<CareShift, CareShiftResponse>()
            .ForCtorParam(nameof(CareShiftResponse.PatientMedicalCode),
                o => o.MapFrom(s => s.Patient != null ? s.Patient.MedicalCode : string.Empty))
            .ForCtorParam(nameof(CareShiftResponse.IsFull), o => o.MapFrom(s => s.CurrentCount >= s.Capacity))
            .ForCtorParam(nameof(CareShiftResponse.Tags), o => o.MapFrom(s => s.Tags.Select(t => t.Name).ToList()))
            .ForCtorParam(nameof(CareShiftResponse.Caregivers), o => o.MapFrom(s => s.Assignments));

        CreateMap<ShiftSwapRequest, SwapRequestResponse>()
            .ForCtorParam(nameof(SwapRequestResponse.RequestorName),
                o => o.MapFrom(s => s.Requestor != null ? s.Requestor.FullName : string.Empty))
            .ForCtorParam(nameof(SwapRequestResponse.TargetCaregiverName),
                o => o.MapFrom(s => s.TargetCaregiver != null ? s.TargetCaregiver.FullName : null));

        CreateMap<Caregiver, CaregiverSummaryResponse>();
    }
}

public static class PatientResponseExtensions
{
    /// <summary>מסמן אם הצופה הוא מנהל החולה.</summary>
    public static PatientResponse ForViewer(this PatientResponse response, int viewerCaregiverId) =>
        response with { IsManager = response.ManagerCaregiverId == viewerCaregiverId };
}
