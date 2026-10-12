# PatientCare – מערכת משמרות לבני משפחה של חולה

מערכת ל-.NET 8 שבה בני משפחה מתאמים ביניהם משמרות סיעוד סביב חולה: כל חולה הוא "מעגל" של בני משפחה,
המנהל של החולה יוצר משמרות, ובני המשפחה במעגל משתבצים אליהן, מבקשים החלפות ומאשרים או דוחים אחד לשני.

## המשאב המוגבל והתחרות עליו

**המשאב המוגבל הוא המקומות הפנויים במשמרת** (`CareShift.Capacity` מול `CareShift.CurrentCount`).
לדוגמה: משמרת עם מקום אחד, ושני בני משפחה לוחצים "שבץ אותי" באותה שנייה.

**איפה יכולה להיות תחרות:**

- שני בני משפחה מתחרים על המקום האחרון באותה משמרת (`POST /api/CareShifts/{id}/register`).
- שתי הצטרפויות או הרשמות במקביל עם אותו קוד או אימייל (נתפס באינדקס ייחודי).
- אישור ודחייה של אותה בקשת החלפה באותו רגע.

**איך המערכת מגיבה:**

- ל-`CareShift` יש עמודת `RowVersion` (`[Timestamp]`), שהיא אסימון Optimistic Concurrency.
  אם בן משפחה אחד שמר ראשון, השמירה של השני נכשלת ב-`DbUpdateConcurrencyException`.
- ה-`UnitOfWork` הופך את החריגה ל-`ConcurrencyException` של שכבת ה-Core (כך ששכבת השירות לא תלויה ב-EF Core).
- השירות תופס אותה, רושם אזהרה בלוג ומחזיר `ServiceErrorType.Conflict`, וה-API מחזיר **409 Conflict**
  עם הודעה בעברית: "המקום במשמרת נתפס זה עתה על ידי בן משפחה אחר. רענן ונסה שוב."
- אף פעם לא נוצר שיבוץ יתר: `CurrentCount` לא עובר את `Capacity`.
- הוכחה בבדיקות: `ConcurrencyTests` (שני `DbContext` שקוראים את אותה שורה, הראשון מצליח והשני מקבל Conflict).

## ארכיטקטורה

| פרויקט | תפקיד |
|---|---|
| `PatientCare.Core` | ישויות, DTOs עם Data Annotations, `ServiceResult`, `PagedResult`, וכל ה-Interfaces (שירותים ו-Repositories) |
| `PatientCare.Infrastructure` | `AppDbContext`, Configurations, מיגרציות, Repositories, `UnitOfWork`, `DbSeeder` |
| `PatientCare.Service` | לוגיקה עסקית, AutoMapper (`MappingProfile`), JWT |
| `API` (`PatientCare.Api`) | Controllers, Middleware, `Program.cs` |
| `PatientCare.Tests` | xUnit + Moq + EF InMemory |

התלות הולכת רק פנימה: `API → Service → Core` ו-`Infrastructure → Core`. שכבת השירות לא מכירה את EF Core בכלל,
והיא עובדת מול `IPatientRepository` וחבריו ומול `IUnitOfWork`.

עקרונות עיקריים:

- **Repository + Unit of Work**: כל גישה לנתונים עוברת ב-Repository, והשמירה ב-`IUnitOfWork.SaveChangesAsync`.
- **Dependency Injection**: הכול נרשם ב-`Program.cs` עם Scoped (מופע לכל בקשה, על אותו `DbContext`).
- **AutoMapper**: כל ההמרות בין ישויות ל-DTOs נמצאות ב-`MappingProfile`.
- **עימוד אמיתי**: `Skip/Take` בתוך השאילתה מול בסיס הנתונים, יחד עם סינון, חיפוש ומיון. הרשימות מחזירות `PagedResult`.
- **Eager loading**: `Include/ThenInclude` בשאילתה אחת, ושאילתות קריאה בלבד עם `AsNoTracking`.
- **CancellationToken** בכל הדרך מה-Controller עד בסיס הנתונים.
- **ולידציה**: Data Annotations על ה-DTOs (חוזרת 400 בפורמט `{ message, errors }`) וכללים עסקיים בשירות.

## קודי תשובה

| קוד | מתי |
|---|---|
| 200 | קריאה או פעולה שהצליחה |
| 201 | יצירה (חולה, משמרת, בקשת החלפה, הרשמה) |
| 204 | ביטול בקשת החלפה (`DELETE /api/SwapRequests/{id}`) |
| 400 | קלט לא תקין או הפרת כלל עסקי |
| 401 | לא מחובר או סיסמה שגויה |
| 403 | מחובר אבל אין הרשאה (לא מנהל החולה, לא `Admin`) |
| 404 | לא נמצא, או חולה או משמרת מחוץ למעגל שלך |
| 409 | תחרות על המשמרת, או משמרת חופפת |

## תפקידים והרשאות

`CaregiverRole`: `User` (ברירת מחדל) ו-`Admin`. התפקיד נכנס לאסימון ה-JWT כ-claim.
נקודות הקצה תחת `/api/Admin` מוגנות ב-`[Authorize(Roles = "Admin")]`. משתמש רגיל מקבל 403.
שינוי תפקיד נכנס לתוקף בהתחברות הבאה של אותו משתמש.

## בסיס נתונים: PostgreSQL

המערכת עובדת מול PostgreSQL (Npgsql). מקומית אפשר להרים בסיס נתונים עם Docker:

```powershell
docker compose up -d
```

מחרוזת ההתחברות המקומית נמצאת ב-`API/appsettings.json`. אפשר לעקוף אותה עם `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..." --project API`.

**פעם אחת, אחרי המעבר מ-SQL Server**: יוצרים מיגרציה חדשה ל-PostgreSQL (המיגרציות הישנות של SQL Server הוחלפו בהערה):

```powershell
dotnet ef migrations add InitialCreate --project PatientCare.Infrastructure --startup-project API
```

הפקודה לא מתחברת לבסיס הנתונים. את הקבצים החדשים שנוצרו בתיקיית `Migrations` צריך להעלות ל-git, והשרת מחיל אותם לבד בעליית השרת.

## פריסה ל-Render

1. מעלים את התיקייה (שמכילה את `patientcare-backend` ואת `patientcare-frontend`, יחד עם `render.yaml`) ל-GitHub.
2. ב-Render: New, Blueprint, ובוחרים את המאגר. נוצרים בסיס PostgreSQL, שירות API (Docker) ואתר סטטי.
3. ב-API מגדירים `Cors__AllowedOrigins` ככתובת האתר (למשל `https://patientcare-web.onrender.com`).
4. באתר מגדירים `VITE_API_URL` ככתובת ה-API עם `/api` בסוף (למשל `https://patientcare-api.onrender.com/api`), ומריצים Deploy מחדש לאתר.
5. `Jwt__Key` נוצר אוטומטית. לנתוני דמו או מנהל מערכת מגדירים `Seed__DemoPassword` ו-`Seed__AdminEmail`.

נקודת בדיקת תקינות: `GET /health`. בתוכנית החינמית של Render בסיס הנתונים נמחק אחרי כשלושים יום, והשרת "נרדם" כשאין תנועה.

## הרצה

```powershell
cd PatientCare-Dotnet-main

# 1. מפתח ה-JWT לא נשמר בקוד. שומרים אותו ב-User Secrets (פעם אחת):
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית ארוכה, לפחות 32 תווים>" --project API

# 2. (אופציונלי) נתוני דמו ומנהל מערכת:
dotnet user-secrets set "Seed:DemoPassword" "<סיסמה לחשבונות הדמו>" --project API
dotnet user-secrets set "Seed:AdminEmail" "<האימייל שלך, כדי להפוך אותו למנהל מערכת>" --project API

# 3. הרצה. בסביבת Development המיגרציות מוחלות והדמו נזרע אוטומטית:
dotnet run --project API
```

אם `Jwt:Key` לא הוגדר בסביבת Development, המערכת יוצרת מפתח זמני לריצה הנוכחית בלבד (ובסביבה אחרת היא לא עולה).

חשבונות הדמו (כשמוגדרת סיסמת דמו): `admin@patientcare.local` (מנהלת מערכת), `rachel@patientcare.local`, `david@patientcare.local`.

הפעלת מיגרציות ידנית:

```powershell
dotnet ef database update --project PatientCare.Infrastructure --startup-project API
```

## בדיקות

```powershell
dotnet test
```

הבדיקות כוללות: מצבי תחרות (`ConcurrencyTests`), כללי מעגל המשפחה (`FamilyAccessTests`), עימוד ומנהל מערכת
(`PagingAndAdminTests`), בקשות החלפה מקצה לקצה (`SwapRequestTests`), בדיקות יחידה עם Moq (`MockedServiceTests`) ובדיקת AutoMapper (`MappingTests`).

## לוגים

NLog כותב ל-`logs/patientcare-all-<תאריך>.log` ולקונסול. כל שורה כוללת `CorrelationId` שמגיע מהכותרת `X-Correlation-Id`
(או נוצר חדש ומוחזר בתשובה). ה-`RequestLoggingMiddleware` רושם כל בקשה עם קוד תשובה וזמן, ו-`ExceptionHandlingMiddleware` מחזיר JSON אחיד לשגיאות.
סדר ה-Middleware: CorrelationId ← RequestLogging ← ExceptionHandling ← Authentication ← Authorization.
