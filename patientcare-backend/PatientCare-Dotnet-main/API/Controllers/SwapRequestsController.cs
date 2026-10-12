using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientCare.Core.DTOs;
using PatientCare.Core.Interfaces;

namespace API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class SwapRequestsController : ApiControllerBase
{
    private readonly ISwapRequestService _swapRequestService;

    public SwapRequestsController(ISwapRequestService swapRequestService)
    {
        _swapRequestService = swapRequestService;
    }

    /// <summary>בקשת החלפה. מחזיר 201.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SwapRequestResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SwapRequestResponse>> CreateSwapRequest([FromBody] CreateSwapRequestDto request, CancellationToken ct)
    {
        var result = await _swapRequestService.CreateSwapRequestAsync(User.GetCaregiverId(), request, ct);
        return CreatedFromResult(result, nameof(GetSwapRequestById), r => new { id = r.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SwapRequestResponse>> GetSwapRequestById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _swapRequestService.GetSwapRequestByIdAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<SwapRequestResponse>> ApproveSwapRequest([FromRoute] int id, CancellationToken ct)
    {
        var result = await _swapRequestService.ApproveSwapRequestAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>דחיית בקשה, עם סיבה או בלי. רק בן המשפחה במשמרת היעד.</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<SwapRequestResponse>> RejectSwapRequest(
        [FromRoute] int id,
        [FromBody] RejectSwapRequestDto? request,
        CancellationToken ct)
    {
        var result = await _swapRequestService.RejectSwapRequestAsync(id, User.GetCaregiverId(), request?.Reason, ct);
        return FromResult(result);
    }

    /// <summary>ביטול בקשה שלי שעדיין ממתינה. מחזיר 204.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> CancelSwapRequest([FromRoute] int id, CancellationToken ct)
    {
        var result = await _swapRequestService.CancelSwapRequestAsync(id, User.GetCaregiverId(), ct);
        return NoContentFromResult(result);
    }

    [HttpGet("pending-for-me")]
    public async Task<ActionResult<List<SwapRequestResponse>>> GetPendingSwapRequestsForMe(CancellationToken ct)
    {
        var result = await _swapRequestService.GetPendingSwapRequestsForManagerAsync(User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>בקשות החלפה ממתינות שאני צריך לאשר (אני בן המשפחה במשמרת היעד).</summary>
    [HttpGet("incoming")]
    public async Task<ActionResult<List<SwapRequestResponse>>> GetIncoming(CancellationToken ct)
    {
        var result = await _swapRequestService.GetIncomingSwapRequestsAsync(User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>בקשות ההחלפה ששלחתי, עם הסטטוס שלהן.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<SwapRequestResponse>>> GetMine(CancellationToken ct)
    {
        var result = await _swapRequestService.GetMySwapRequestsAsync(User.GetCaregiverId(), ct);
        return FromResult(result);
    }
}
