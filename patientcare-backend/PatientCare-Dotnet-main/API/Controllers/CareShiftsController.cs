using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Interfaces;

namespace API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class CareShiftsController : ApiControllerBase
{
    private readonly ICareShiftService _careShiftService;

    public CareShiftsController(ICareShiftService careShiftService)
    {
        _careShiftService = careShiftService;
    }

    /// <summary>משמרות של החולים שאני חבר במעגל שלהם. עימוד וסינון בשרת.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CareShiftResponse>>> GetShifts([FromQuery] CareShiftQuery query, CancellationToken ct)
    {
        return Ok(await _careShiftService.GetShiftsAsync(User.GetCaregiverId(), query, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CareShiftResponse>> GetShiftById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _careShiftService.GetShiftByIdAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>יצירת משמרת. מנהל החולה בלבד. מחזיר 201.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CareShiftResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CareShiftResponse>> CreateShift([FromBody] CreateCareShiftRequest request, CancellationToken ct)
    {
        var result = await _careShiftService.CreateShiftAsync(request, User.GetCaregiverId(), ct);
        return CreatedFromResult(result, nameof(GetShiftById), s => new { id = s.Id });
    }

    /// <summary>משבץ אותי למקום פנוי במשמרת. מחזיר 409 בתחרות על המקום האחרון.</summary>
    [HttpPost("{id:int}/register")]
    public async Task<ActionResult<CareShiftResponse>> RegisterForShift([FromRoute] int id, CancellationToken ct)
    {
        var result = await _careShiftService.RegisterCaregiverForShiftAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }
}
