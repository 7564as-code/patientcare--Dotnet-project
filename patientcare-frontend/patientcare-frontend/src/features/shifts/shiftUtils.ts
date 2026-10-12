import type { CareShiftResponse } from '@/types/dto';

export type ShiftState = 'empty' | 'partial' | 'full';

export const shiftState = (s: CareShiftResponse): ShiftState =>
  s.currentCount === 0 ? 'empty' : s.isFull ? 'full' : 'partial';

export const shiftStateLabel: Record<ShiftState, string> = {
  empty: 'אין בן משפחה משובץ',
  partial: 'יש מקום פנוי',
  full: 'מלאה',
};
