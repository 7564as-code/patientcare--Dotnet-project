using AutoMapper;
using Microsoft.Extensions.Logging;
using PatientCare.Core.Common;
using PatientCare.Core.DTOs;
using PatientCare.Core.Enums;
using PatientCare.Core.Interfaces;
using PatientCare.Service.Mapping;

namespace PatientCare.Service.Services;

public class AdminService : IAdminService
{
    private readonly ICaregiverRepository _caregivers;
    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ICaregiverRepository caregivers,
        IPatientRepository patients,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AdminService> logger)
    {
        _caregivers = caregivers;
        _patients = patients;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CaregiverSummaryResponse>> GetCaregiversAsync(PageQuery query, CancellationToken ct = default)
    {
        var page = await _caregivers.GetPagedAsync(query, ct);
        return page.WithItems(_mapper.Map<List<CaregiverSummaryResponse>>(page.Items));
    }

    public async Task<PagedResult<PatientResponse>> GetAllPatientsAsync(PatientQuery query, int adminId, CancellationToken ct = default)
    {
        var page = await _patients.GetAllPagedAsync(query, ct);
        return page.WithItems(page.Items
            .Select(p => _mapper.Map<PatientResponse>(p).ForViewer(adminId))
            .ToList());
    }

    public async Task<ServiceResult<CaregiverSummaryResponse>> ChangeRoleAsync(int caregiverId, CaregiverRole role, int actingAdminId, CancellationToken ct = default)
    {
        var caregiver = await _caregivers.GetByIdAsync(caregiverId, ct);
        if (caregiver is null)
        {
            return ServiceResult<CaregiverSummaryResponse>.Failure(ServiceErrorType.NotFound, "בן המשפחה לא נמצא.");
        }

        // מונע מצב שבו אף אחד לא נשאר מנהל מערכת.
        if (caregiverId == actingAdminId && role != CaregiverRole.Admin)
        {
            return ServiceResult<CaregiverSummaryResponse>.Failure(
                ServiceErrorType.Validation,
                "אי אפשר להסיר את הרשאת המנהל מעצמך.");
        }

        if (caregiver.Role != role)
        {
            caregiver.Role = role;
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Admin {AdminId} changed role of caregiver {CaregiverId} to {Role}.", actingAdminId, caregiverId, role);
        }

        return ServiceResult<CaregiverSummaryResponse>.Success(_mapper.Map<CaregiverSummaryResponse>(caregiver));
    }
}
