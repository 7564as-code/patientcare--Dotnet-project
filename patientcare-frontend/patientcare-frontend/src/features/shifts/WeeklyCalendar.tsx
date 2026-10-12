import { useEffect, useMemo, useRef } from 'react';
import type { CareShiftResponse } from '@/types/dto';
import { addDays, formatTime, formatWeekdayShort, isSameDay, parseApiDate } from '@/utils/dates';
import { segmentsForDay, visibleHourRange } from './calendarLayout';
import { shiftState } from './shiftUtils';
import s from './WeeklyCalendar.module.css';

const HOUR_H = 56;

interface Props {
  weekStart: Date;
  shifts: CareShiftResponse[];
  selectedId: number | null;
  onSelect: (shift: CareShiftResponse) => void;
  now: Date;
  myId: number | null;
}

export function WeeklyCalendar({ weekStart, shifts, selectedId, onSelect, now, myId }: Props) {
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)), [weekStart]);
  const perDay = useMemo(() => days.map((d) => segmentsForDay(shifts, d)), [days, shifts]);
  const [fromH, toH] = useMemo(() => visibleHourRange(perDay), [perDay]);
  const hours = Array.from({ length: toH - fromH }, (_, i) => fromH + i);
  const bodyH = hours.length * HOUR_H;
  const scrollRef = useRef<HTMLDivElement>(null);

  // פותח את הלוח סביב המשמרת הראשונה בשבוע (ולא בחצות), כדי שהתוכן יופיע מיד.
  useEffect(() => {
    const starts = perDay.flat().filter((x) => !x.continuesFromPrev).map((x) => x.startMin);
    const first = starts.length ? Math.min(...starts) : fromH * 60;
    if (scrollRef.current) scrollRef.current.scrollTop = Math.max(0, ((first / 60 - fromH - 1) * HOUR_H));
  }, [perDay, fromH]);

  const nowMin = now.getHours() * 60 + now.getMinutes();
  const nowTop = ((nowMin - fromH * 60) / 60) * HOUR_H;

  return (
    <div className={s.cal} role="grid" aria-label="לוח משמרות שבועי">
      <div className={s.scroll} ref={scrollRef}>
        <div className={s.head} role="row">
          <div className={s.corner} />
          {days.map((d) => {
            const today = isSameDay(d, now);
            return (
              <div key={d.toISOString()} className={`${s.dayHead} ${today ? s.todayHead : ''}`} role="columnheader">
                <span className={s.dow}>{formatWeekdayShort(d)}</span>
                <span className={`${s.dom} num`}>{d.getDate()}</span>
              </div>
            );
          })}
        </div>

        <div className={s.body} style={{ height: bodyH }}>
          <div className={s.gutter}>
            {hours.map((h, i) => (
              <span key={h} className={`${s.hour} num`} style={{ top: i * HOUR_H }}>{String(h).padStart(2, '0')}:00</span>
            ))}
          </div>

          {days.map((d, di) => {
            const today = isSameDay(d, now);
            return (
              <div key={d.toISOString()} className={`${s.col} ${today ? s.todayCol : ''}`} role="gridcell">
                {hours.map((h, i) => <div key={h} className={s.line} style={{ top: i * HOUR_H }} />)}
                {today && nowTop >= 0 && nowTop <= bodyH && <div className={s.now} style={{ top: nowTop }} aria-hidden />}

                {perDay[di].map((seg) => {
                  const sh = seg.shift;
                  const st = shiftState(sh);
                  const top = ((seg.startMin - fromH * 60) / 60) * HOUR_H;
                  const height = Math.max(((seg.endMin - seg.startMin) / 60) * HOUR_H - 3, 28);
                  const width = 100 / seg.lanes;
                  const roomy = height > 84 && seg.lanes < 3;
                  const isMine = sh.caregivers.some((c) => c.caregiverId === myId);
                  const names = sh.caregivers.map((c) => (c.caregiverId === myId ? 'אני' : c.fullName.split(' ')[0])).join(', ');
                  const fullNames = sh.caregivers.map((c) => c.fullName).join(', ');
                  const openSlots = Math.max(sh.capacity - sh.currentCount, 0);
                  const who = sh.caregivers.length === 0
                    ? 'פנויה, אף אחד לא תפס'
                    : `תפוס: ${names}${openSlots > 0 ? ` · ${openSlots} פנוי` : ''}`;
                  const showWho = height >= 58;
                  return (
                    <button
                      key={`${sh.id}-${di}`}
                      className={[
                        s.shift, s[st],
                        selectedId === sh.id ? s.selected : '',
                        seg.continuesFromPrev ? s.cutTop : '',
                        seg.continuesToNext ? s.cutBottom : '',
                        isMine ? s.mine : '',
                      ].join(' ')}
                      style={{ top, height, insetInlineStart: `${seg.lane * width}%`, width: `calc(${width}% - 4px)` }}
                      onClick={() => onSelect(sh)}
                      aria-pressed={selectedId === sh.id}
                      title={`${sh.patientMedicalCode} · ${formatTime(parseApiDate(sh.startTime))}–${formatTime(parseApiDate(sh.endTime))} · ${sh.currentCount}/${sh.capacity}${fullNames ? ` · ${fullNames}` : ''}`}
                      aria-label={`משמרת של ${sh.patientMedicalCode}, ${formatTime(parseApiDate(sh.startTime))} עד ${formatTime(parseApiDate(sh.endTime))}, ${sh.currentCount} מתוך ${sh.capacity} בני משפחה${isMine ? ', אתה משובץ' : ''}`}
                    >
                      <span className={s.time}>
                        <span className="num">{formatTime(parseApiDate(sh.startTime))}</span>
                        <span className={s.dash}>–</span>
                        <span className="num">{formatTime(parseApiDate(sh.endTime))}</span>
                      </span>
                      <span className={s.patient}>{sh.patientMedicalCode}</span>
                      {isMine && <span className={s.me}>אני</span>}
                      {showWho && <span className={s.names}>{who}</span>}
                      {roomy && <span className={`${s.count} num`}>{sh.currentCount}/{sh.capacity}</span>}
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
