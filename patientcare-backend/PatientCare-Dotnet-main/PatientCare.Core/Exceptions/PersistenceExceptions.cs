namespace PatientCare.Core.Exceptions;

/// <summary>
/// נזרקת כששמירה נכשלה בגלל התנגשות אופטימית (מישהו אחר שינה את השורה בינתיים).
/// שכבת הנתונים ממירה אליה את DbUpdateConcurrencyException, כדי ששכבת השירות לא תהיה תלויה ב-EF Core.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>נזרקת כששמירה נכשלה בגלל הפרת אילוץ בבסיס הנתונים (למשל אינדקס ייחודי).</summary>
public class DataConstraintException : Exception
{
    public DataConstraintException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
