import axios, { AxiosError } from 'axios';
import { tokenStorage } from '@/auth/tokenStorage';
import type { ApiErrorBody } from '@/types/dto';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
});

let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(fn: (() => void) | null) {
  onUnauthorized = fn;
}

api.interceptors.request.use((config) => {
  const auth = tokenStorage.get();
  if (auth?.token) config.headers.Authorization = `Bearer ${auth.token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error: AxiosError<ApiErrorBody>) => {
    // ה-Backend מחזיר 401 גם כשגיאה עסקית (למשל "רק בן המשפחה במשמרת היעד יכול לאשר") — אז יש body עם message.
    // 401 של JWT פג/לא תקין מגיע ללא body. רק אותו נתייחס כניתוק.
    const isAuthFailure = error.response?.status === 401 && !error.response.data?.message;
    if (isAuthFailure && tokenStorage.get()) onUnauthorized?.();
    return Promise.reject(error);
  },
);

const FALLBACKS: Record<number, string> = {
  400: 'הבקשה אינה תקינה.',
  401: 'אין הרשאה לביצוע הפעולה.',
  403: 'אין הרשאה לביצוע הפעולה.',
  404: 'הפריט המבוקש לא נמצא.',
  409: 'הפעולה התנגשה עם שינוי אחר. רענן ונסה שוב.',
  500: 'אירעה שגיאה בשרת. נסה שוב בעוד רגע.',
};

/** מחלץ הודעת שגיאה קריאה מתוך תשובת ה-API (שדה message) או מחזיר טקסט כללי. */
export function getErrorMessage(err: unknown): string {
  if (axios.isAxiosError<ApiErrorBody>(err)) {
    if (!err.response) return 'אין חיבור לשרת. בדוק שה-API פועל ושכתובת ה-API נכונה.';
    const d = err.response.data;
    if (d?.message) return d.message;
    if (d?.title) return d.title;
    return FALLBACKS[err.response.status] ?? 'אירעה שגיאה לא צפויה.';
  }
  return err instanceof Error ? err.message : 'אירעה שגיאה לא צפויה.';
}
