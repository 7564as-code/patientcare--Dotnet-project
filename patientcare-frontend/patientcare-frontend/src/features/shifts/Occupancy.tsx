import type { CareShiftResponse } from '@/types/dto';
import s from './Occupancy.module.css';

/** נקודות איכלוס: מלאה = בן משפחה משובץ, ריקה = מקום פנוי. */
export function Occupancy({ shift, showText = true }: { shift: CareShiftResponse; showText?: boolean }) {
  const cap = Math.min(shift.capacity, 8);
  return (
    <span className={s.wrap} title={`${shift.currentCount} מתוך ${shift.capacity} בני משפחה`}>
      <span className={s.dots} aria-hidden>
        {Array.from({ length: cap }, (_, i) => (
          <i key={i} className={i < shift.currentCount ? s.on : ''} />
        ))}
      </span>
      {showText && (
        <span className={s.text}>
          <span className="num">{shift.currentCount}/{shift.capacity}</span> בני משפחה
        </span>
      )}
    </span>
  );
}
