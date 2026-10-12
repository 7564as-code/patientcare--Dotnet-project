using PatientCare.Core.Common;
using PatientCare.Core.DTOs;

namespace PatientCare.Core.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
