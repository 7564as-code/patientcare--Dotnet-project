using System.Security.Cryptography;
using AutoMapper;
using Microsoft.Extensions.Logging;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Exceptions;
using PatientCare.Core.Interfaces;
using PatientCare.Service.Mapping;

namespace PatientCare.Service.Services;

public class PatientService : IPatientService
{
    // בלי תווים מבלבלים (0/O, 1/I).
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository patients,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _patients = patients;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<PatientResponse>> GetPatientsAsync(int caregiverId, PatientQuery query, CancellationToken ct = default)
    {
        var page = await _patients.GetVisiblePagedAsync(caregiverId, query, ct);
        return page.WithItems(page.Items.Select(p => ToResponse(p, caregiverId)).ToList());
    }

    public Task<List<string>> GetDepartmentsAsync(int caregiverId, CancellationToken ct = default) =>
        _patients.GetVisibleDepartmentsAsync(caregiverId, ct);

    public async Task<ServiceResult<PatientResponse>> GetPatientByIdAsync(int id, int caregiverId, CancellationToken ct = default)
    {
        var patient = await _patients.GetVisibleByIdAsync(id, caregiverId, ct);
        if (patient is null)
        {
            return ServiceResult<PatientResponse>.Failure(ServiceErrorType.NotFound, "החולה לא נמצא.");
        }

        return ServiceResult<PatientResponse>.Success(ToResponse(patient, caregiverId));
    }

    public async Task<ServiceResult<PatientResponse>> CreatePatientAsync(
        CreatePatientRequest request,
        int managerCaregiverId,
        CancellationToken ct = default)
    {
        if (request.DateOfBirth.Date > DateTime.UtcNow.Date)
        {
            return ServiceResult<PatientResponse>.Failure(ServiceErrorType.Validation, "תאריך הלידה לא יכול להיות בעתיד.");
        }

        // השם צריך להיות ייחודי רק בין החולים של אותו מנהל.
        if (await _patients.ManagerHasPatientNamedAsync(managerCaregiverId, request.MedicalCode, ct))
        {
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Validation,
                "כבר קיים אצלך חולה עם השם הזה.");
        }

        var patient = new Patient
        {
            MedicalCode = request.MedicalCode,
            PatientGender = request.PatientGender,
            DateOfBirth = request.DateOfBirth,
            Department = request.Department,
            ManagerCaregiverId = managerCaregiverId,
            InviteCode = await GenerateUniqueInviteCodeAsync(ct)
        };

        _patients.Add(patient);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} created by caregiver {CaregiverId}.", patient.Id, managerCaregiverId);
        return ServiceResult<PatientResponse>.Success(ToResponse(patient, managerCaregiverId));
    }

    public async Task<ServiceResult<PatientResponse>> ChangeManagerAsync(
        int patientId,
        int currentManagerId,
        int newManagerCaregiverId,
        CancellationToken ct = default)
    {
        var patient = await _patients.GetByIdWithMembersAsync(patientId, ct);
        if (patient is null)
        {
            return ServiceResult<PatientResponse>.Failure(ServiceErrorType.NotFound, "החולה לא נמצא.");
        }

        // רק המנהל הנוכחי יכול להעביר את הניהול.
        if (patient.ManagerCaregiverId != currentManagerId)
        {
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Forbidden,
                "רק מנהל החולה יכול להעביר את הניהול.");
        }

        if (newManagerCaregiverId == currentManagerId)
        {
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Validation,
                "אתה כבר מנהל החולה הזה.");
        }

        // המנהל החדש חייב להיות בן משפחה שכבר הצטרף למעגל של החולה.
        var newManagerMembership = patient.Members.FirstOrDefault(m => m.CaregiverId == newManagerCaregiverId);
        if (newManagerMembership is null)
        {
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Validation,
                "המנהל החדש חייב להיות בן משפחה שהצטרף למעגל של החולה.");
        }

        // המנהל החדש הופך למנהל (חבר מובלע), והמנהל הקודם נשאר חבר רגיל במעגל.
        _patients.RemoveMember(newManagerMembership);
        patient.Members.Add(new PatientMember { CaregiverId = currentManagerId });
        patient.ManagerCaregiverId = newManagerCaregiverId;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Manager of patient {PatientId} changed from {OldManagerId} to {NewManagerId}.",
            patientId, currentManagerId, newManagerCaregiverId);

        return ServiceResult<PatientResponse>.Success(ToResponse(patient, currentManagerId));
    }

    public async Task<ServiceResult<PatientResponse>> JoinAsync(string inviteCode, int caregiverId, CancellationToken ct = default)
    {
        var code = (inviteCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            return ServiceResult<PatientResponse>.Failure(ServiceErrorType.Validation, "יש להזין קוד הזמנה.");
        }

        var patient = await _patients.GetByInviteCodeAsync(code, ct);
        if (patient is null)
        {
            return ServiceResult<PatientResponse>.Failure(ServiceErrorType.NotFound, "קוד ההזמנה אינו תקין.");
        }

        var alreadyInCircle = patient.ManagerCaregiverId == caregiverId
            || await _patients.HasMembershipRowAsync(patient.Id, caregiverId, ct);
        if (alreadyInCircle)
        {
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Validation,
                "אתה כבר חבר במעגל של החולה הזה.");
        }

        _patients.AddMember(new PatientMember { PatientId = patient.Id, CaregiverId = caregiverId });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DataConstraintException)
        {
            // הצטרפות כפולה במקביל (האינדקס הייחודי).
            return ServiceResult<PatientResponse>.Failure(
                ServiceErrorType.Validation,
                "אתה כבר חבר במעגל של החולה הזה.");
        }

        _logger.LogInformation("Caregiver {CaregiverId} joined the circle of patient {PatientId}.", caregiverId, patient.Id);
        return ServiceResult<PatientResponse>.Success(ToResponse(patient, caregiverId));
    }

    public async Task<ServiceResult<List<PatientMemberResponse>>> GetMembersAsync(int patientId, int caregiverId, CancellationToken ct = default)
    {
        var patient = await _patients.GetVisibleWithMembersDetailsAsync(patientId, caregiverId, ct);
        if (patient is null)
        {
            return ServiceResult<List<PatientMemberResponse>>.Failure(ServiceErrorType.NotFound, "החולה לא נמצא.");
        }

        var members = new List<PatientMemberResponse>();
        if (patient.ManagerCaregiver is not null)
        {
            members.Add(new PatientMemberResponse(patient.ManagerCaregiver.Id, patient.ManagerCaregiver.FullName, true));
        }

        members.AddRange(patient.Members
            .Where(m => m.CaregiverId != patient.ManagerCaregiverId)
            .OrderBy(m => m.Caregiver.FullName)
            .Select(m => new PatientMemberResponse(m.CaregiverId, m.Caregiver.FullName, false)));

        return ServiceResult<List<PatientMemberResponse>>.Success(members);
    }

    public async Task<ServiceResult<InviteCodeResponse>> GetInviteCodeAsync(int patientId, int caregiverId, bool reset, CancellationToken ct = default)
    {
        var patient = await _patients.GetVisibleByIdTrackedAsync(patientId, caregiverId, ct);
        if (patient is null)
        {
            return ServiceResult<InviteCodeResponse>.Failure(ServiceErrorType.NotFound, "החולה לא נמצא.");
        }

        if (patient.ManagerCaregiverId != caregiverId)
        {
            return ServiceResult<InviteCodeResponse>.Failure(
                ServiceErrorType.Forbidden,
                "רק מנהל החולה יכול לנהל את קוד ההזמנה.");
        }

        // חולים שנוצרו לפני הוספת הקודים מקבלים קוד בפעם הראשונה שהמנהל מבקש אותו.
        if (reset || patient.InviteCode is null)
        {
            patient.InviteCode = await GenerateUniqueInviteCodeAsync(ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Invite code of patient {PatientId} was (re)generated.", patientId);
        }

        return ServiceResult<InviteCodeResponse>.Success(new InviteCodeResponse(patient.InviteCode!));
    }

    private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken ct)
    {
        while (true)
        {
            var chars = new char[CodeLength];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }

            var code = new string(chars);
            if (!await _patients.InviteCodeExistsAsync(code, ct))
            {
                return code;
            }
        }
    }

    private PatientResponse ToResponse(Patient patient, int viewerCaregiverId) =>
        _mapper.Map<PatientResponse>(patient).ForViewer(viewerCaregiverId);
}
