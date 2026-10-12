# PatientCare – Frontend

React + TypeScript + Vite, מול ה-.NET API הקיים. ממשק RTL בעברית, Responsive (מחשב/טלפון).

## הרצה

```bash
npm install
cp .env.example .env.development   # אם אין לך כבר
npm run dev                        # http://localhost:5173
npm run build                      # בדיקת טיפוסים + build
```

1. הרץ את ה-API (פרופיל `http` ב-`launchSettings.json` → `http://localhost:5229`).
2. `VITE_API_URL=/api` – בפיתוח הקריאות עוברות דרך proxy של Vite אל `VITE_DEV_PROXY_TARGET`.
   ה-API **לא מגדיר CORS** ב-`Program.cs`, לכן ה-proxy הוא הדרך הפשוטה בפיתוח.
   לפרודקשן: הגדר `VITE_API_URL=https://your-host/api` והוסף CORS ב-Backend.

## מבנה

```
src/
  api/        client.ts (Axios + interceptors), auth/patients/shifts/swapRequests .api.ts
  auth/       AuthContext, ProtectedRoute, פענוח JWT, tokenStorage
  types/      enums.ts + dto.ts – משקפים 1:1 את ה-DTOs ב-PatientCare.Core
  components/ ui/ (Button, Modal, Toast, Badge…), layout/AppShell
  features/   shifts/ (WeeklyCalendar, DayAgenda, ShiftDetails, SwapDialog), patients/
  pages/      Login, Dashboard, Patients, PatientDetails, Shifts, SwapRequests
```

## מודל המערכת: מעגל משפחתי לכל חולה

- יוצר החולה הוא **מנהל** המעגל. רק הוא יוצר משמרות, מזמין בני משפחה ומעביר ניהול.
- בני משפחה מצטרפים עם **קוד הזמנה** ("הצטרפות עם קוד" במסך החולים).
- כל אחד רואה רק חולים שהוא מנהל או חבר במעגל שלהם, ורק את המשמרות שלהם.
- בלוח המשמרות רואים **מי משובץ**, ומשמרות שלי מסומנות ב"אני". בקשת החלפה אפשרית רק ממשמרת שאני משובץ בה.

## Endpoints בשימוש

| Endpoint | שימוש |
|---|---|
| `POST /api/Auth/register`, `POST /api/Auth/login` | הרשמה והתחברות |
| `GET/POST /api/Patients`, `GET /api/Patients/{id}` | חולים |
| `POST /api/Patients/join` | הצטרפות עם קוד הזמנה |
| `GET /api/Patients/{id}/members` | בני המשפחה במעגל |
| `GET /api/Patients/{id}/invite-code`, `POST .../invite-code/reset` | קוד הזמנה (מנהל) |
| `PUT /api/Patients/{id}/manager` | העברת ניהול (לחבר במעגל) |
| `GET/POST /api/CareShifts`, `POST /api/CareShifts/{id}/register` | משמרות (יצירה: מנהל) |
| `POST /api/SwapRequests` – `{ careShiftId, targetCareShiftId, reason }` | בקשת החלפה (ללא `targetCaregiverId`) |
| `GET /api/SwapRequests/incoming`, `GET /api/SwapRequests/mine`, `POST /api/SwapRequests/{id}/approve` | טיפול בבקשות |

- Enums מגיעים/נשלחים כמספרים – ראה `types/enums.ts`.
- 401 עם `{ message }` הוא שגיאה עסקית ולא פקיעת טוקן – לא מנתקים עליו.
- הודעות השגיאה מגיעות מה-Backend כבר בעברית.
