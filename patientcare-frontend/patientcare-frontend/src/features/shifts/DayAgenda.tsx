import { useMemo } from 'react';
import { CalendarOff } from 'lucide-react';
import type { CareShiftResponse } from '@/types/dto';
import { Badge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/States';
import { addDays, formatTime, formatWeekday, formatWeekdayShort, isSameDay, parseApiDate } from '@/utils/dates';
import { genderRequirementLabel } from '@/utils/labels';
import { segmentsForDay } from './calendarLayout';
import { shiftState, shiftStateLabel } from './shiftUtils';
import { Occupancy } from './Occupancy';
import s from './DayAgenda.module.css';

interface Props {
  weekStart: Date;
  shifts: CareShiftResponse[];
  day: Date;
  onDayChange: (d: Date) => void;
  selectedId: number | null;
  onSelect: (shift: CareShiftResponse) => void;
  now: Date;
  myId: number | null;
}

/** תצוגת מובייל: פס ימים + רשימת משמרות של היום הנבחר. */
export function DayAgenda({ weekStart, shifts, day, onDayChange, selectedId, onSelect, now, myId }: Props) {
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)), [weekStart]);
  const segs = useMemo(() => segmentsForDay(shifts, day), [shifts, day]);

  return (
    <div>
      <div className={s.strip} role="tablist" aria-label="ימי השבוע">
        {days.map((d) => {
          const count = segmentsForDay(shifts, d).length;
          const active = isSameDay(d, day);
          return (
            <button key={d.toISOString()} role="tab" aria-selected={active} className={`${s.day} ${active ? s.active : ''} ${isSameDay(d, now) ? s.today : ''}`} onClick={() => onDayChange(d)}>
              <span className={s.dow}>{formatWeekdayShort(d)}</span>
              <span className={`${s.dom} num`}>{d.getDate()}</span>
              <span className={s.pip} style={{ opacity: count ? 1 : 0 }} />
            </button>
          );
        })}
      </div>

      <h2 className={s.title}>{formatWeekday(day)}</h2>

      {segs.length === 0 ? (
        <EmptyState icon={<CalendarOff size={22} />} title="אין משמרות ביום הזה" hint="עבור ליום אחר או לשבוע אחר כדי לראות משמרות." />
      ) : (
        <ul className={s.list}>
          {segs.map(({ shift: sh }) => {
            const st = shiftState(sh);
            return (
              <li key={sh.id}>
                <button className={`${s.card} ${s[st]} ${selectedId === sh.id ? s.selected : ''}`} onClick={() => onSelect(sh)}>
                  <div className={s.times}>
                    <strong className="num">{formatTime(parseApiDate(sh.startTime))}</strong>
                    <span className="num muted">{formatTime(parseApiDate(sh.endTime))}</span>
                  </div>
                  <div className={s.main}>
                    <span className={s.patient}>{sh.patientMedicalCode}</span>
                    <Occupancy shift={sh} />
                    {sh.caregivers.length > 0 && (
                      <span className={s.names}>{sh.caregivers.map((c) => (c.caregiverId === myId ? 'אתה' : c.fullName)).join(', ')}</span>
                    )}
                    <div className={s.tags}>
                      <Badge tone={st === 'full' ? 'indigo' : st === 'partial' ? 'brand' : 'amber'}>{shiftStateLabel[st]}</Badge>
                      <Badge>{genderRequirementLabel(sh.requiredGender)}</Badge>
                    </div>
                  </div>
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
