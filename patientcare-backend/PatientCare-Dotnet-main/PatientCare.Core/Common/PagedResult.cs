namespace PatientCare.Core.Common;

/// <summary>תוצאת עימוד: עמוד אחד של פריטים, ומספר הפריטים הכולל במערכת.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>אותו עימוד עם פריטים אחרים (למשל אחרי מיפוי ל-DTO).</summary>
    public PagedResult<TOut> WithItems<TOut>(IReadOnlyList<TOut> items) => new(items, Page, PageSize, TotalCount);
}
