using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Interfaces;

namespace API.Controllers;

/// <summary>פעולות למנהל מערכת בלבד (Role = Admin). משתמש רגיל מקבל 403.</summary>
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ApiControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>כל בני המשפחה במערכת, בעימוד.</summary>
    [HttpGet("caregivers")]
    public async Task<ActionResult<PagedResult<CaregiverSummaryResponse>>> GetCaregivers([FromQuery] PageQuery query, CancellationToken ct)
    {
        return Ok(await _adminService.GetCaregiversAsync(query, ct));
    }

    /// <summary>כל החולים במערכת, בעימוד.</summary>
    [HttpGet("patients")]
    public async Task<ActionResult<PagedResult<PatientResponse>>> GetPatients([FromQuery] PatientQuery query, CancellationToken ct)
    {
        return Ok(await _adminService.GetAllPatientsAsync(query, User.GetCaregiverId(), ct));
    }

    /// <summary>שינוי תפקיד של בן משפחה. התפקיד החדש נכנס לתוקף בהתחברות הבאה שלו.</summary>
    [HttpPut("caregivers/{id:int}/role")]
    public async Task<ActionResult<CaregiverSummaryResponse>> ChangeRole(
        [FromRoute] int id,
        [FromBody] ChangeCaregiverRoleRequest request,
        CancellationToken ct)
    {
        var result = await _adminService.ChangeRoleAsync(id, request.Role, User.GetCaregiverId(), ct);
        return FromResult(result);
    }
}
