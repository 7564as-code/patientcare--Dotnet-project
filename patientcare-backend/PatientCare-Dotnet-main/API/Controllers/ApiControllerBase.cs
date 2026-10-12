using Microsoft.AspNetCore.Mvc;
using PatientCare.Core.Common;

namespace API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>200 בהצלחה, אחרת קוד שגיאה לפי סוג השגיאה העסקית.</summary>
    protected ActionResult<T> FromResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ErrorFor(result);
    }

    /// <summary>201 Created (עם כותרת Location) בהצלחה, אחרת קוד שגיאה.</summary>
    protected ActionResult<T> CreatedFromResult<T>(ServiceResult<T> result, string actionName, Func<T, object> routeValues)
    {
        if (result.Succeeded)
        {
            return CreatedAtAction(actionName, routeValues(result.Value!), result.Value);
        }

        return ErrorFor(result);
    }

    /// <summary>204 No Content בהצלחה, אחרת קוד שגיאה.</summary>
    protected ActionResult NoContentFromResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        return ErrorFor(result).Result!;
    }

    private ActionResult<T> ErrorFor<T>(ServiceResult<T> result)
    {
        var body = new { message = result.Error };

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(body),
            ServiceErrorType.Validation => BadRequest(body),
            ServiceErrorType.Conflict => Conflict(body),
            ServiceErrorType.Unauthorized => Unauthorized(body),
            ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            _ => Problem(result.Error)
        };
    }
}
