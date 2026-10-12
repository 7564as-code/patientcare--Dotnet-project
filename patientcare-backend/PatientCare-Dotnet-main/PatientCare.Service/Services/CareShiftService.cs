using AutoMapper;
using Microsoft.Extensions.Logging;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Exceptions;
using PatientCare.Core.Interfaces;

namespace PatientCare.Service.Services;

public class CareShiftService : ICareShiftService
{
    private const int MaxTagLength = 50;

    private readonly ICareShiftRepository _shifts;
    private readonly IPatientRepository _patients;
    private readonly ICaregiverRepository _caregivers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CareShiftService> _logger;

    public CareShiftService(
        ICareShiftRepository shifts,
        IPatientRepository patients,
        ICaregiverRepository caregivers,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CareShiftService> logger)
    {
        _shifts = shifts;
        _patients = patients;
        _caregivers = caregivers;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CareShiftResponse>> GetShiftsAsync(int caregiverId, CareShiftQuery query, CancellationToken ct = default)
    {
        var page = await _shifts.GetVisiblePagedAsync(caregiverId, query, ct);
        return page.WithItems(_mapper.Map<List<CareShiftResponse>>(page.Items));
    }

    public async Task<ServiceResult<CareShiftResponse>> GetShiftByIdAsync(int id, int caregiverId, CancellationToken ct = default)
    {
        var shift = await _shifts.GetVisibleByIdAsync(id, caregiverId, ct);
        if (shift is null)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.NotFound, "המשמרת לא נמצאה.");
        }

        return ServiceResult<CareShiftResponse>.Success(_mapper.Map<CareShiftResponse>(shift));
    }

    public async Task<ServiceResult<CareShiftResponse>> CreateShiftAsync(CreateCareShiftRequest request, int caregiverId, CancellationToken ct = default)
    {
        var patient = await _patients.GetVisibleByIdTrackedAsync(request.PatientId, caregiverId, ct);
        if (patient is null)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.NotFound, "החולה לא נמצא.");
        }

        // רק מנהל החולה יכול ליצור משמרות.
        if (patient.ManagerCaregiverId != caregiverId)
        {
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Forbidden,
                "רק מנהל החולה יכול ליצור משמרות.");
        }

        if (request.EndTime <= request.StartTime)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.Validation, "שעת הסיום חייבת להיות אחרי שעת ההתחלה.");
        }

        if (request.Capacity <= 0)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.Validation, "מספר בני המשפחה במשמרת חייב להיות גדול מאפס.");
        }

        var tagNames = (request.Tags ?? new List<string>())
            .Select(t => (t ?? string.Empty).Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        if (tagNames.Any(t => t.Length > MaxTagLength))
        {
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Validation,
                $"תגית יכולה להכיל עד {MaxTagLength} תווים.");
        }

        // אי אפשר ליצור משמרת שחופפת בזמן למשמרת קיימת של אותו חולה.
        if (await _shifts.HasOverlapAsync(request.PatientId, request.StartTime, request.EndTime, ct))
        {
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Conflict,
                "כבר קיימת משמרת של החולה הזה בזמן הזה. בחר שעות אחרות.");
        }

        var shift = new CareShift
        {
            PatientId = request.PatientId,
            Patient = patient,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequiredGender = request.RequiredGender,
            Capacity = request.Capacity,
            CurrentCount = 0
        };

        if (tagNames.Count > 0)
        {
            var existingTags = await _shifts.GetTagsByNamesAsync(tagNames, ct);
            var newTagNames = tagNames.Except(existingTags.Select(t => t.Name));

            foreach (var name in newTagNames)
            {
                var tag = new Tag { Name = name };
                _shifts.AddTag(tag);
                existingTags.Add(tag);
            }

            foreach (var tag in existingTags)
            {
                shift.Tags.Add(tag);
            }
        }

        _shifts.Add(shift);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DataConstraintException)
        {
            // למשל שתי יצירות במקביל של אותה תגית חדשה.
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Conflict,
                "השמירה נכשלה בגלל פעולה מקבילה. נסה שוב.");
        }

        _logger.LogInformation("Shift {ShiftId} created for patient {PatientId}.", shift.Id, shift.PatientId);
        return ServiceResult<CareShiftResponse>.Success(_mapper.Map<CareShiftResponse>(shift));
    }

    public async Task<ServiceResult<CareShiftResponse>> RegisterCaregiverForShiftAsync(int shiftId, int caregiverId, CancellationToken ct = default)
    {
        var caregiver = await _caregivers.GetByIdAsync(caregiverId, ct);
        if (caregiver is null)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.NotFound, "בן המשפחה לא נמצא.");
        }

        var shift = await _shifts.GetByIdWithDetailsAsync(shiftId, ct);
        if (shift is null)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.NotFound, "המשמרת לא נמצאה.");
        }

        // רק בן משפחה שחבר במעגל של החולה יכול להשתבץ למשמרות שלו.
        if (!await _patients.IsMemberAsync(shift.PatientId, caregiverId, ct))
        {
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Forbidden,
                "אינך חבר במעגל של החולה הזה.");
        }

        if (shift.Assignments.Any(a => a.CaregiverId == caregiverId))
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.Validation, "אתה כבר משובץ למשמרת הזו.");
        }

        if (!shift.CanAssignCaregiver(caregiver))
        {
            var reason = shift.IsFull()
                ? "במשמרת אין יותר מקומות פנויים."
                : "מין בן המשפחה אינו תואם לדרישת המשמרת.";
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.Validation, reason);
        }

        shift.CurrentCount++;
        shift.Assignments.Add(new ShiftAssignment
        {
            CareShiftId = shift.Id,
            CaregiverId = caregiverId,
            Caregiver = caregiver
        });

        try
        {
            // RowVersion (concurrency token) נבדק כאן: אם בן משפחה אחר תפס את המקום האחרון ושמר
            // קודם, השמירה תיכשל ותחזור כ-ConcurrencyException.
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "Concurrency conflict while caregiver {CaregiverId} registered for shift {ShiftId}.",
                caregiverId, shiftId);
            return ServiceResult<CareShiftResponse>.Failure(
                ServiceErrorType.Conflict,
                "המקום במשמרת נתפס זה עתה על ידי בן משפחה אחר. רענן ונסה שוב.");
        }
        catch (DataConstraintException)
        {
            return ServiceResult<CareShiftResponse>.Failure(ServiceErrorType.Validation, "אתה כבר משובץ למשמרת הזו.");
        }

        _logger.LogInformation("Caregiver {CaregiverId} registered for shift {ShiftId}.", caregiverId, shiftId);
        return ServiceResult<CareShiftResponse>.Success(_mapper.Map<CareShiftResponse>(shift));
    }
}
