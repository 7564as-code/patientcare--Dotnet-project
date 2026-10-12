namespace PatientCare.Core.Common;

public enum ServiceErrorType
{
    None,
    NotFound,
    Validation,
    Conflict,
    /// <summary>לא מחובר או פרטי התחברות שגויים (401).</summary>
    Unauthorized,
    /// <summary>מחובר אבל אין הרשאה לפעולה (403).</summary>
    Forbidden
}

/// <summary>Outcome wrapper so services can signal typed failures (e.g. Conflict for a lost concurrency race) without throwing across layers.</summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public ServiceErrorType ErrorType { get; }
    public string? Error { get; }

    private ServiceResult(bool succeeded, T? value, ServiceErrorType errorType, string? error)
    {
        Succeeded = succeeded;
        Value = value;
        ErrorType = errorType;
        Error = error;
    }

    public static ServiceResult<T> Success(T value) => new(true, value, ServiceErrorType.None, null);

    public static ServiceResult<T> Failure(ServiceErrorType errorType, string error) =>
        new(false, default, errorType, error);
}
