import { useMemo, useState } from 'react';
import { ArrowDown, Search } from 'lucide-react';
import type { CareShiftResponse } from '@/types/dto';
import { getErrorMessage, swapRequestsApi } from '@/api';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { addDays, formatDayMonth, formatWeekday, parseApiDate, startOfDay, timeRange } from '@/utils/dates';
import { Occupancy } from './Occupancy';
import s from './SwapDialog.module.css';

interface Props {
  open: boolean;
  /** המשמרת שלי, שממנה אני רוצה לעבור */
  ownShift: CareShiftResponse;
  myId: number | null;
  allShifts: CareShiftResponse[];
  onClose: () => void;
  onCreated: () => void;
}

const describe = (sh: CareShiftResponse) => {
  const st = parseApiDate(sh.startTime);
  return `${formatWeekday(st)} ${formatDayMonth(st)} · ${timeRange(st, parseApiDate(sh.endTime))}`;
};
const names = (sh: CareShiftResponse) => sh.caregivers.map((c) => c.fullName).join(', ');

const pad = (n: number) => String(n).padStart(2, '0');
const toDateInput = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
/** 'YYYY-MM-DD' כתאריך מקומי (ולא UTC). */
const fromDateInput = (v: string) => {
  const [y, m, d] = v.split('-').map(Number);
  return new Date(y, m - 1, d);
};
const minutesOfDay = (d: Date) => d.getHours() * 60 + d.getMinutes();
const timeToMinutes = (v: string) => {
  const [h, m] = v.split(':').map(Number);
  return h * 60 + m;
};

export function SwapDialog({ open, ownShift, myId, allShifts, onClose, onCreated }: Props) {
  const [targetId, setTargetId] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [q, setQ] = useState('');
  const [dateFilter, setDateFilter] = useState('');
  const [timeFilter, setTimeFilter] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // משמרת יעד: עתידית, עם בן משפחה משובץ (אחרת אין עם מי להחליף), ושאני לא משובץ בה.
  const candidates = useMemo(() => {
    const now = Date.now();
    const t = q.trim().toLowerCase();
    const day = dateFilter ? fromDateInput(dateFilter) : null;
    const dayEnd = day ? addDays(startOfDay(day), 1) : null;
    const tm = timeFilter ? timeToMinutes(timeFilter) : null;

    // התאמה לתאריך ולשעה שנבחרו: המשמרת חלה ביום הזה ו/או פעילה בשעה הזו.
    const matchesWhen = (x: CareShiftResponse) => {
      const st = parseApiDate(x.startTime);
      const en = parseApiDate(x.endTime);
      if (day && dayEnd && tm != null) {
        const instant = new Date(day.getFullYear(), day.getMonth(), day.getDate(), Math.floor(tm / 60), tm % 60);
        return st <= instant && instant < en;
      }
      if (day && dayEnd) return st < dayEnd && en > day;
      if (tm != null) {
        const sm = minutesOfDay(st);
        const em = minutesOfDay(en);
        return sm <= em ? sm <= tm && tm < em : tm >= sm || tm < em;
      }
      return true;
    };

    return allShifts
      .filter((x) => x.id !== ownShift.id && x.caregivers.length > 0 && parseApiDate(x.endTime).getTime() > now)
      .filter((x) => !x.caregivers.some((c) => c.caregiverId === myId))
      .filter(matchesWhen)
      .filter((x) => !t || x.patientMedicalCode.toLowerCase().includes(t) || names(x).toLowerCase().includes(t) || describe(x).includes(t))
      .sort((a, b) => parseApiDate(a.startTime).getTime() - parseApiDate(b.startTime).getTime());
  }, [allShifts, ownShift.id, myId, q, dateFilter, timeFilter]);

  async function submit() {
    if (targetId == null) return;
    setBusy(true);
    setError(null);
    try {
      // לא שולחים targetCaregiverId — ה-Backend קובע אותו לפי משמרת היעד.
      await swapRequestsApi.create({
        careShiftId: ownShift.id,
        targetCareShiftId: targetId,
        reason: reason.trim() || undefined,
      });
      setTargetId(null);
      setReason('');
      onCreated();
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  const picked = candidates.find((c) => c.id === targetId);

  return (
    <Modal
      open={open}
      onClose={onClose}
      wide
      title="בקשת החלפת משמרת"
      footer={
        <>
          <Button loading={busy} disabled={targetId == null} onClick={submit}>שליחת בקשה</Button>
          <Button variant="ghost" onClick={onClose}>ביטול</Button>
        </>
      }
    >
      <div className={s.flow}>
        <div className={s.own}>
          <span className={s.label}>המשמרת שלי</span>
          <strong>{describe(ownShift)}</strong>
          <span className={s.code}>{ownShift.patientMedicalCode}</span>
        </div>
        <div className={s.arrow} aria-hidden><ArrowDown size={18} /></div>

        <div>
          <span className={s.label}>לאיזו משמרת תרצה לעבור?</span>
          <div className={s.filters}>
            <div className="field">
              <label htmlFor="swap-date">תאריך</label>
              <input
                id="swap-date"
                type="date"
                className="input"
                min={toDateInput(new Date())}
                value={dateFilter}
                onChange={(e) => setDateFilter(e.target.value)}
              />
              {dateFilter && <span className="field-hint">יום {formatWeekday(fromDateInput(dateFilter))}</span>}
            </div>
            <div className="field">
              <label htmlFor="swap-time">שעה</label>
              <input
                id="swap-time"
                type="time"
                className="input"
                value={timeFilter}
                onChange={(e) => setTimeFilter(e.target.value)}
              />
            </div>
            {(dateFilter || timeFilter) && (
              <Button
                variant="ghost"
                onClick={() => {
                  setDateFilter('');
                  setTimeFilter('');
                }}
              >
                ניקוי
              </Button>
            )}
          </div>
          <div className={s.search}>
            <Search size={16} aria-hidden />
            <input className="input" placeholder="חיפוש לפי חולה, בן משפחה או יום" value={q} onChange={(e) => setQ(e.target.value)} aria-label="חיפוש משמרת יעד" />
          </div>
          <ul className={s.list} role="radiogroup" aria-label="משמרות יעד">
            {candidates.length === 0 && <li className={s.none}>אין משמרות מתאימות לבחירה (בדוק תאריך/שעה אחרים, או נקה את הסינון).</li>}
            {candidates.map((c) => (
              <li key={c.id}>
                <label className={`${s.option} ${targetId === c.id ? s.picked : ''}`}>
                  <input type="radio" name="target" checked={targetId === c.id} onChange={() => setTargetId(c.id)} />
                  <span className={s.optMain}>
                    <strong>{describe(c)}</strong>
                    <span className="muted">{c.patientMedicalCode} · {names(c)}</span>
                  </span>
                  <Occupancy shift={c} showText={false} />
                </label>
              </li>
            ))}
          </ul>
        </div>

        <div className="field">
          <label htmlFor="swap-reason">סיבה (לא חובה)</label>
          <textarea id="swap-reason" className="input" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="למשל: אירוע משפחתי באותו יום" maxLength={500} />
        </div>

        <p className="field-hint">
          {picked
            ? `הבקשה תישלח ל${picked.caregivers[0]?.fullName ?? 'בן המשפחה במשמרת היעד'}. כשיאשר, ההחלפה תתבצע אוטומטית בין שתי המשמרות.`
            : 'כשבן המשפחה במשמרת היעד יאשר, ההחלפה תתבצע אוטומטית בין שתי המשמרות.'}
        </p>
        {error && <div role="alert" className={s.error}>{error}</div>}
      </div>
    </Modal>
  );
}
