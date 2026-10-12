const HE = 'he-IL';

/** תאריכי ה-API מגיעים ללא אזור זמן (DateTime ב-.NET) — מפורשים כזמן מקומי. */
export const parseApiDate = (s: string) => new Date(s);

export function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

export function addDays(d: Date, n: number): Date {
  const x = new Date(d);
  x.setDate(x.getDate() + n);
  return x;
}

/** תחילת שבוע ישראלי: יום ראשון. */
export function startOfWeek(d: Date): Date {
  const x = startOfDay(d);
  x.setDate(x.getDate() - x.getDay());
  return x;
}

export const isSameDay = (a: Date, b: Date) =>
  a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();

export const formatTime = (d: Date) => d.toLocaleTimeString(HE, { hour: '2-digit', minute: '2-digit', hour12: false });
export const formatWeekday = (d: Date) => d.toLocaleDateString(HE, { weekday: 'long' });
export const formatWeekdayShort = (d: Date) => d.toLocaleDateString(HE, { weekday: 'short' });
export const formatDayMonth = (d: Date) => d.toLocaleDateString(HE, { day: 'numeric', month: 'short' });
export const formatFullDate = (d: Date) => d.toLocaleDateString(HE, { day: 'numeric', month: 'long', year: 'numeric' });
export const formatDateTime = (d: Date) => `${formatDayMonth(d)}, ${formatTime(d)}`;

export function formatWeekRange(start: Date): string {
  const end = addDays(start, 6);
  const sameMonth = start.getMonth() === end.getMonth();
  const month = (d: Date) => d.toLocaleDateString(HE, { month: 'long' });
  const year = end.getFullYear();
  return sameMonth
    ? `${start.getDate()} עד ${end.getDate()} ב${month(end)} ${year}`
    : `${start.getDate()} ב${month(start)} עד ${end.getDate()} ב${month(end)} ${year}`;
}

export function formatDuration(start: Date, end: Date): string {
  const mins = Math.round((end.getTime() - start.getTime()) / 60000);
  const h = Math.floor(mins / 60);
  const m = mins % 60;
  if (h && m) return `${h} שע׳ ${m} דק׳`;
  return h ? `${h} שעות` : `${m} דקות`;
}

export function relativeDay(d: Date, now = new Date()): string | null {
  if (isSameDay(d, now)) return 'היום';
  if (isSameDay(d, addDays(now, 1))) return 'מחר';
  return null;
}

/** טווח שעות כמחרוזת LTR מבודדת, כדי שלא יתהפך בתוך טקסט עברי. */
export const timeRange = (a: Date, b: Date) => `\u2066${formatTime(a)}–${formatTime(b)}\u2069`;
