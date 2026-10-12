using System.ComponentModel.DataAnnotations;
using PatientCare.Core.Enums;

namespace PatientCare.Core.DTOs;

/// <summary>פרמטרי עימוד, חיפוש ומיון משותפים לרשימות. מגיעים מה-query string.</summary>
public class PageQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "מספר העמוד חייב להיות 1 או יותר.")]
    public int Page { get; set; } = 1;

    [Range(1, 200, ErrorMessage = "גודל העמוד חייב להיות בין 1 ל-200.")]
    public int PageSize { get; set; } = 20;

    [StringLength(100, ErrorMessage = "טקסט החיפוש ארוך מדי.")]
    public string? Search { get; set; }

    public bool Descending { get; set; }
}

public class PatientQuery : PageQuery
{
    /// <summary>סינון לפי מקום טיפול מדויק.</summary>
    [StringLength(100)]
    public string? Department { get; set; }

    public PatientSortBy SortBy { get; set; } = PatientSortBy.Name;
}

public class CareShiftQuery : PageQuery
{
    public int? PatientId { get; set; }

    /// <summary>משמרות שמסתיימות אחרי התאריך הזה.</summary>
    public DateTime? From { get; set; }

    /// <summary>משמרות שמתחילות לפני התאריך הזה.</summary>
    public DateTime? To { get; set; }

    /// <summary>רק משמרות שעוד יש בהן מקום פנוי.</summary>
    public bool OnlyWithOpenSlots { get; set; }
}
