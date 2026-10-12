# שינויים ב-Backend: מערכת משפחתית (מעגל לכל חולה)

## חובה אחרי העתקת הקבצים: Migration

מהתיקייה שבה נמצא קובץ ה-sln:

```bash
dotnet ef migrations add AddFamilyCircles --project PatientCare.Infrastructure --startup-project API
dotnet ef database update --project PatientCare.Infrastructure --startup-project API
```

ה-migration יוצר את הטבלה `PatientMembers`, מוסיף את `Patients.InviteCode`,
וממיר את האינדקס של `MedicalCode` מייחודי לרגיל.

**חולים קיימים:** המנהל נחשב חבר באופן מובלע, ולכן אין צורך להעביר נתונים. חולה ישן שאין לו
`ManagerCaregiverId` (NULL) לא יהיה גלוי לאף אחד. לתיקון:
`UPDATE Patients SET ManagerCaregiverId = <מזהה בן משפחה> WHERE ManagerCaregiverId IS NULL;`
קוד הזמנה לחולים ישנים נוצר בפעם הראשונה שהמנהל מבקש אותו.

## קבצים חדשים
- `PatientCare.Core/Entities/PatientMember.cs`
- `PatientCare.Infrastructure/Data/Configurations/PatientMemberConfiguration.cs`
- `PatientCare.Service/Services/PatientAccess.cs`: כלל הגישה (מנהל או חבר)
- `PatientCare.Tests/FamilyAccessTests.cs`

## קבצים ששונו
- `Entities/Patient.cs` (`InviteCode`, `Members`), `Entities/Caregiver.cs` (הערה)
- `DTOs/PatientDtos.cs`, `DTOs/CareShiftDtos.cs` (`Caregivers`), `DTOs/SwapRequestDtos.cs` (`TargetCareShiftId`)
- `Data/AppDbContext.cs`, `Data/Configurations/PatientConfiguration.cs`
- `Interfaces/IPatientService`, `ICareShiftService`, `ISwapRequestService`
- `Services/PatientService`, `CareShiftService`, `SwapRequestService`, `AuthService` (הודעות)
- `Controllers/PatientsController`, `CareShiftsController`, `SwapRequestsController`
- `Middlewares/ExceptionHandlingMiddleware.cs` (הודעות)
- `Tests/CareShiftServiceTests.cs`, `Tests/ConcurrencyTests.cs`

## כללי הגישה החדשים
| פעולה | מי רשאי |
|---|---|
| רשימת חולים / חולה / משמרות | מנהל או חבר במעגל |
| יצירת חולה | כל משתמש (הופך למנהל) |
| יצירת משמרת | **מנהל החולה בלבד** |
| הרשמה למשמרת | חבר במעגל |
| הצטרפות | מי שיש לו קוד הזמנה |
| קוד הזמנה, העברת ניהול | מנהל בלבד (המנהל החדש חייב להיות חבר) |
| בקשת החלפה | שני הצדדים חברים בשני המעגלים |

## Endpoints חדשים
`POST /api/Patients/join`, `GET /api/Patients/{id}/members`, `GET /api/Patients/{id}/invite-code`,
`POST /api/Patients/{id}/invite-code/reset`, `GET /api/SwapRequests/incoming`, `GET /api/SwapRequests/mine`.

## הערה על שמות
שמות מחלקות, שדות וטבלאות בקוד (Patient, Caregiver, MedicalCode, Department) לא שונו, כדי לא לשבור את
ה-DB וה-API. כל הטקסטים שמוצגים למשתמש עברו לעברית עם "חולה" ו"בן משפחה".
`MedicalCode` משמש עכשיו כשם החולה, ו-`Department` כמקום.
