using AutoMapper;
using Microsoft.Extensions.Logging;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Core.Exceptions;
using PatientCare.Core.Interfaces;

namespace PatientCare.Service.Services;

public class SwapRequestService : ISwapRequestService
{
    private const int MaxReasonLength = 500;

    private readonly ISwapRequestRepository _swapRequests;
    private readonly ICareShiftRepository _shifts;
    private readonly IPatientRepository _patients;
    private readonly ICaregiverRepository _caregivers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SwapRequestService> _logger;

    public SwapRequestService(
        ISwapRequestRepository swapRequests,
        ICareShiftRepository shifts,
        IPatientRepository patients,
        ICaregiverRepository caregivers,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SwapRequestService> logger)
    {
        _swapRequests = swapRequests;
        _shifts = shifts;
        _patients = patients;
        _caregivers = caregivers;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<SwapRequestResponse>> CreateSwapRequestAsync(int requestorId, CreateSwapRequestDto request, CancellationToken ct = default)
    {
        var requestor = await _caregivers.GetByIdAsync(requestorId, ct);
        if (requestor is null)
        {
            return Fail(ServiceErrorType.NotFound, "בן המשפחה לא נמצא.");
        }

        var shift = await _shifts.GetByIdAsync(request.CareShiftId, ct);
        if (shift is null)
        {
            return Fail(ServiceErrorType.NotFound, "המשמרת לא נמצאה.");
        }

        var targetShift = await _shifts.GetByIdWithAssignmentsAsync(request.TargetCareShiftId, ct);
        if (targetShift is null)
        {
            return Fail(ServiceErrorType.NotFound, "משמרת היעד לא נמצאה.");
        }

        var targetAssignment = targetShift.Assignments.FirstOrDefault();
        if (targetAssignment is null)
        {
            return Fail(ServiceErrorType.Validation, "במשמרת היעד אין בן משפחה משובץ להחלפה.");
        }

        if (targetAssignment.CaregiverId == requestorId)
        {
            return Fail(ServiceErrorType.Validation, "אתה כבר משובץ למשמרת היעד.");
        }

        if (!await _shifts.IsAssignedAsync(request.CareShiftId, requestorId, ct))
        {
            return Fail(ServiceErrorType.Validation, "אתה לא משובץ למשמרת שבחרת. בחר משמרת שלך.");
        }

        // ההחלפה מכניסה כל אחד מהצדדים למשמרת של החולה של הצד השני, ולכן שניהם
        // חייבים להיות חברים במעגל של שני החולים (כשמדובר בחולים שונים).
        if (!await _patients.IsMemberAsync(targetShift.PatientId, requestorId, ct))
        {
            return Fail(ServiceErrorType.Forbidden, "אינך חבר במעגל של החולה במשמרת היעד.");
        }

        if (!await _patients.IsMemberAsync(shift.PatientId, targetAssignment.CaregiverId, ct))
        {
            return Fail(ServiceErrorType.Validation, "בן המשפחה במשמרת היעד אינו חבר במעגל של החולה במשמרת שלך.");
        }

        var targetCaregiver = await _caregivers.GetByIdAsync(targetAssignment.CaregiverId, ct);
        if (targetCaregiver is null)
        {
            return Fail(ServiceErrorType.NotFound, "בן המשפחה המשובץ למשמרת היעד לא נמצא.");
        }

        // בדיקת התאמה לפני שליחת הבקשה: כל צד נכנס למשמרת של הצד השני,
        // ולכן המין של כל אחד חייב להתאים לדרישת המשמרת שאליה הוא עובר.
        if (!requestor.IsMatchingGender(targetShift.RequiredGender))
        {
            return Fail(
                ServiceErrorType.Validation,
                $"אי אפשר לבקש החלפה: משמרת היעד מיועדת ל{GenderRequirementLabel(targetShift.RequiredGender)}, והמין שלך אינו תואם.");
        }

        if (!targetCaregiver.IsMatchingGender(shift.RequiredGender))
        {
            return Fail(
                ServiceErrorType.Validation,
                $"אי אפשר לבקש החלפה: המשמרת שלך מיועדת ל{GenderRequirementLabel(shift.RequiredGender)}, ובן המשפחה במשמרת היעד אינו מתאים לה.");
        }

        var swapRequest = new ShiftSwapRequest
        {
            RequestorId = requestorId,
            Requestor = requestor,
            TargetCaregiverId = targetAssignment.CaregiverId,
            TargetCaregiver = targetCaregiver,
            CareShiftId = request.CareShiftId,
            TargetCareShiftId = request.TargetCareShiftId,
            Reason = request.Reason,
            Status = SwapRequestStatus.Pending
        };

        _swapRequests.Add(swapRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Swap request {SwapRequestId} created by caregiver {RequestorId} (shift {ShiftId} for shift {TargetShiftId}).",
            swapRequest.Id, requestorId, request.CareShiftId, request.TargetCareShiftId);

        return Ok(swapRequest);
    }

    public async Task<ServiceResult<SwapRequestResponse>> GetSwapRequestByIdAsync(int swapRequestId, int caregiverId, CancellationToken ct = default)
    {
        var swapRequest = await _swapRequests.GetByIdWithDetailsAsync(swapRequestId, ct);
        if (swapRequest is null)
        {
            return Fail(ServiceErrorType.NotFound, "בקשת ההחלפה לא נמצאה.");
        }

        // רואים בקשה רק מי ששלח אותה או מי שהיא מיועדת אליו.
        if (swapRequest.RequestorId != caregiverId && swapRequest.TargetCaregiverId != caregiverId)
        {
            return Fail(ServiceErrorType.NotFound, "בקשת ההחלפה לא נמצאה.");
        }

        return Ok(swapRequest);
    }

    public async Task<ServiceResult<SwapRequestResponse>> ApproveSwapRequestAsync(int swapRequestId, int approverCaregiverId, CancellationToken ct = default)
    {
        var approver = await _caregivers.GetByIdAsync(approverCaregiverId, ct);
        if (approver is null)
        {
            return Fail(ServiceErrorType.NotFound, "בן המשפחה לא נמצא.");
        }

        var swapRequest = await _swapRequests.GetByIdWithDetailsAsync(swapRequestId, ct);
        if (swapRequest is null)
        {
            return Fail(ServiceErrorType.NotFound, "בקשת ההחלפה לא נמצאה.");
        }

        if (swapRequest.Status != SwapRequestStatus.Pending)
        {
            return Fail(ServiceErrorType.Validation, "הבקשה כבר טופלה.");
        }

        if (swapRequest.TargetCaregiverId != approverCaregiverId)
        {
            return Fail(ServiceErrorType.Forbidden, "רק בן המשפחה במשמרת היעד יכול לאשר את הבקשה.");
        }

        var requestorAssignment = await _shifts.GetAssignmentAsync(swapRequest.CareShiftId, swapRequest.RequestorId, ct);
        if (requestorAssignment is null)
        {
            return Fail(ServiceErrorType.Validation, "השיבוץ המקורי כבר לא קיים.");
        }

        var targetAssignment = await _shifts.GetAssignmentAsync(swapRequest.TargetCareShiftId, approverCaregiverId, ct);
        if (targetAssignment is null)
        {
            return Fail(ServiceErrorType.Validation, "אתה כבר לא משובץ למשמרת היעד.");
        }

        if (!swapRequest.Requestor.IsMatchingGender(swapRequest.TargetCareShift.RequiredGender))
        {
            return Fail(ServiceErrorType.Validation, "מין המבקש אינו תואם לדרישת משמרת היעד.");
        }

        if (!approver.IsMatchingGender(swapRequest.CareShift.RequiredGender))
        {
            return Fail(ServiceErrorType.Validation, "מינך אינו תואם לדרישת המשמרת המקורית.");
        }

        // החלפת השיבוצים
        requestorAssignment.CaregiverId = approverCaregiverId;
        requestorAssignment.AssignedAt = DateTime.UtcNow;

        targetAssignment.CaregiverId = swapRequest.RequestorId;
        targetAssignment.AssignedAt = DateTime.UtcNow;

        swapRequest.Status = SwapRequestStatus.Approved;
        swapRequest.ResolvedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while approving swap request {SwapRequestId}.", swapRequestId);
            return Fail(ServiceErrorType.Conflict, "הבקשה או המשמרות שונו בינתיים. רענן ונסה שוב.");
        }
        catch (DataConstraintException ex)
        {
            _logger.LogWarning(ex, "Constraint violation while approving swap request {SwapRequestId}.", swapRequestId);
            return Fail(ServiceErrorType.Conflict, "אי אפשר לבצע את ההחלפה כי אחד הצדדים כבר משובץ למשמרת.");
        }

        _logger.LogInformation("Swap request {SwapRequestId} approved by caregiver {CaregiverId}.", swapRequestId, approverCaregiverId);
        return Ok(swapRequest);
    }

    public async Task<ServiceResult<SwapRequestResponse>> RejectSwapRequestAsync(int swapRequestId, int rejecterCaregiverId, string? reason, CancellationToken ct = default)
    {
        var swapRequest = await _swapRequests.GetByIdWithDetailsAsync(swapRequestId, ct);
        if (swapRequest is null)
        {
            return Fail(ServiceErrorType.NotFound, "בקשת ההחלפה לא נמצאה.");
        }

        if (swapRequest.TargetCaregiverId != rejecterCaregiverId)
        {
            return Fail(ServiceErrorType.Forbidden, "רק בן המשפחה במשמרת היעד יכול לדחות את הבקשה.");
        }

        if (swapRequest.Status != SwapRequestStatus.Pending)
        {
            return Fail(ServiceErrorType.Validation, "הבקשה כבר טופלה.");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > MaxReasonLength })
        {
            return Fail(ServiceErrorType.Validation, $"הסיבה ארוכה מדי (עד {MaxReasonLength} תווים).");
        }

        swapRequest.Status = SwapRequestStatus.Rejected;
        swapRequest.ResponseReason = trimmed;
        swapRequest.ResolvedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while rejecting swap request {SwapRequestId}.", swapRequestId);
            return Fail(ServiceErrorType.Conflict, "הבקשה שונתה בינתיים. רענן ונסה שוב.");
        }

        _logger.LogInformation("Swap request {SwapRequestId} rejected by caregiver {CaregiverId}.", swapRequestId, rejecterCaregiverId);
        return Ok(swapRequest);
    }

    public async Task<ServiceResult<SwapRequestResponse>> CancelSwapRequestAsync(int swapRequestId, int requestorId, CancellationToken ct = default)
    {
        var swapRequest = await _swapRequests.GetByIdWithDetailsAsync(swapRequestId, ct);
        if (swapRequest is null)
        {
            return Fail(ServiceErrorType.NotFound, "בקשת ההחלפה לא נמצאה.");
        }

        if (swapRequest.RequestorId != requestorId)
        {
            return Fail(ServiceErrorType.Forbidden, "רק מי ששלח את הבקשה יכול לבטל אותה.");
        }

        if (swapRequest.Status != SwapRequestStatus.Pending)
        {
            return Fail(ServiceErrorType.Validation, "הבקשה כבר טופלה ואי אפשר לבטל אותה.");
        }

        swapRequest.Status = SwapRequestStatus.Cancelled;
        swapRequest.ResolvedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while cancelling swap request {SwapRequestId}.", swapRequestId);
            return Fail(ServiceErrorType.Conflict, "הבקשה שונתה בינתיים. רענן ונסה שוב.");
        }

        _logger.LogInformation("Swap request {SwapRequestId} cancelled by caregiver {CaregiverId}.", swapRequestId, requestorId);
        return Ok(swapRequest);
    }

    public async Task<ServiceResult<List<SwapRequestResponse>>> GetPendingSwapRequestsForManagerAsync(int managerCaregiverId, CancellationToken ct = default) =>
        ServiceResult<List<SwapRequestResponse>>.Success(
            _mapper.Map<List<SwapRequestResponse>>(await _swapRequests.GetPendingForManagerAsync(managerCaregiverId, ct)));

    public async Task<ServiceResult<List<SwapRequestResponse>>> GetIncomingSwapRequestsAsync(int caregiverId, CancellationToken ct = default) =>
        ServiceResult<List<SwapRequestResponse>>.Success(
            _mapper.Map<List<SwapRequestResponse>>(await _swapRequests.GetIncomingAsync(caregiverId, ct)));

    public async Task<ServiceResult<List<SwapRequestResponse>>> GetMySwapRequestsAsync(int caregiverId, CancellationToken ct = default) =>
        ServiceResult<List<SwapRequestResponse>>.Success(
            _mapper.Map<List<SwapRequestResponse>>(await _swapRequests.GetMineAsync(caregiverId, ct)));

    private ServiceResult<SwapRequestResponse> Ok(ShiftSwapRequest request) =>
        ServiceResult<SwapRequestResponse>.Success(_mapper.Map<SwapRequestResponse>(request));

    private static ServiceResult<SwapRequestResponse> Fail(ServiceErrorType type, string message) =>
        ServiceResult<SwapRequestResponse>.Failure(type, message);

    private static string GenderRequirementLabel(GenderRequirement requirement) => requirement switch
    {
        GenderRequirement.Male => "גברים בלבד",
        GenderRequirement.Female => "נשים בלבד",
        _ => "כולם"
    };
}
