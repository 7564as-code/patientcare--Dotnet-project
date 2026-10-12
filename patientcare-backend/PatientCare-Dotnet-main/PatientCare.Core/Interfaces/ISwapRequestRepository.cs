using PatientCare.Core.Entities;

namespace PatientCare.Core.Interfaces;

public interface ISwapRequestRepository
{
    /// <summary>בקשה עם המשמרות ובני המשפחה שלה, עם מעקב שינויים.</summary>
    Task<ShiftSwapRequest?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>בקשות ממתינות על משמרות של חולים שבן המשפחה מנהל.</summary>
    Task<List<ShiftSwapRequest>> GetPendingForManagerAsync(int managerCaregiverId, CancellationToken ct = default);

    /// <summary>בקשות ממתינות שבהן בן המשפחה הוא היעד.</summary>
    Task<List<ShiftSwapRequest>> GetIncomingAsync(int caregiverId, CancellationToken ct = default);

    /// <summary>כל הבקשות ששלח בן המשפחה.</summary>
    Task<List<ShiftSwapRequest>> GetMineAsync(int caregiverId, CancellationToken ct = default);

    void Add(ShiftSwapRequest request);
}
