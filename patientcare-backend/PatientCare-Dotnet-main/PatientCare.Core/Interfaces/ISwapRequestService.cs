using PatientCare.Core.Common;
using PatientCare.Core.DTOs;

namespace PatientCare.Core.Interfaces;

public interface ISwapRequestService
{
    Task<ServiceResult<SwapRequestResponse>> CreateSwapRequestAsync(int requestorId, CreateSwapRequestDto request, CancellationToken ct = default);

    Task<ServiceResult<SwapRequestResponse>> GetSwapRequestByIdAsync(int swapRequestId, int caregiverId, CancellationToken ct = default);

    Task<ServiceResult<SwapRequestResponse>> ApproveSwapRequestAsync(int swapRequestId, int approverCaregiverId, CancellationToken ct = default);

    /// <summary>דחיית בקשה על ידי בן המשפחה במשמרת היעד, עם סיבה אופציונלית.</summary>
    Task<ServiceResult<SwapRequestResponse>> RejectSwapRequestAsync(int swapRequestId, int rejecterCaregiverId, string? reason, CancellationToken ct = default);

    /// <summary>ביטול בקשה על ידי מי ששלח אותה (רק כשהיא עדיין ממתינה).</summary>
    Task<ServiceResult<SwapRequestResponse>> CancelSwapRequestAsync(int swapRequestId, int requestorId, CancellationToken ct = default);

    Task<ServiceResult<List<SwapRequestResponse>>> GetPendingSwapRequestsForManagerAsync(int managerCaregiverId, CancellationToken ct = default);

    /// <summary>בקשות החלפה ממתינות שבהן בן המשפחה הוא היעד (הוא זה שצריך לאשר).</summary>
    Task<ServiceResult<List<SwapRequestResponse>>> GetIncomingSwapRequestsAsync(int caregiverId, CancellationToken ct = default);

    /// <summary>כל בקשות ההחלפה ששלח בן המשפחה, עם הסטטוס שלהן.</summary>
    Task<ServiceResult<List<SwapRequestResponse>>> GetMySwapRequestsAsync(int caregiverId, CancellationToken ct = default);
}
