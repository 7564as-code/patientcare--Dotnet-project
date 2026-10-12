using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Interfaces;

namespace API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class PatientsController : ApiControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>החולים שאני מנהל או שהצטרפתי למעגל שלהם. עימוד, חיפוש ומיון בשרת.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientResponse>>> GetPatients([FromQuery] PatientQuery query, CancellationToken ct)
    {
        return Ok(await _patientService.GetPatientsAsync(User.GetCaregiverId(), query, ct));
    }

    /// <summary>מקומות הטיפול של החולים שלי (לסינון).</summary>
    [HttpGet("departments")]
    public async Task<ActionResult<List<string>>> GetDepartments(CancellationToken ct)
    {
        return Ok(await _patientService.GetDepartmentsAsync(User.GetCaregiverId(), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientResponse>> GetPatientById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _patientService.GetPatientByIdAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>יצירת חולה. מחזיר 201.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientResponse>> CreatePatient([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var result = await _patientService.CreatePatientAsync(request, User.GetCaregiverId(), ct);
        return CreatedFromResult(result, nameof(GetPatientById), p => new { id = p.Id });
    }

    [HttpPut("{id:int}/manager")]
    public async Task<ActionResult<PatientResponse>> ChangeManager(
        [FromRoute] int id,
        [FromBody] ChangePatientManagerRequest request,
        CancellationToken ct)
    {
        var result = await _patientService.ChangeManagerAsync(
            id,
            User.GetCaregiverId(),
            request.NewManagerCaregiverId,
            ct);

        return FromResult(result);
    }

    /// <summary>הצטרפות למעגל של חולה באמצעות קוד הזמנה.</summary>
    [HttpPost("join")]
    public async Task<ActionResult<PatientResponse>> Join([FromBody] JoinPatientRequest request, CancellationToken ct)
    {
        var result = await _patientService.JoinAsync(request.InviteCode, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>רשימת בני המשפחה במעגל של החולה (המנהל ראשון).</summary>
    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<List<PatientMemberResponse>>> GetMembers([FromRoute] int id, CancellationToken ct)
    {
        var result = await _patientService.GetMembersAsync(id, User.GetCaregiverId(), ct);
        return FromResult(result);
    }

    /// <summary>קוד ההזמנה של החולה. מנהל בלבד.</summary>
    [HttpGet("{id:int}/invite-code")]
    public async Task<ActionResult<InviteCodeResponse>> GetInviteCode([FromRoute] int id, CancellationToken ct)
    {
        var result = await _patientService.GetInviteCodeAsync(id, User.GetCaregiverId(), reset: false, ct);
        return FromResult(result);
    }

    /// <summary>יצירת קוד הזמנה חדש (מבטל את הקודם). מנהל בלבד.</summary>
    [HttpPost("{id:int}/invite-code/reset")]
    public async Task<ActionResult<InviteCodeResponse>> ResetInviteCode([FromRoute] int id, CancellationToken ct)
    {
        var result = await _patientService.GetInviteCodeAsync(id, User.GetCaregiverId(), reset: true, ct);
        return FromResult(result);
    }
}
