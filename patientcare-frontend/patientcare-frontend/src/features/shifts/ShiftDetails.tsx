import { Link } from 'react-router-dom';
import { ArrowLeftRight, CalendarDays, Clock, ExternalLink, Info, UserCheck, UserPlus, UserRound, X } from 'lucide-react';
import type { CareShiftResponse } from '@/types/dto';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { formatDuration, formatFullDate, formatTime, formatWeekday, parseApiDate } from '@/utils/dates';
import { genderRequirementLabel } from '@/utils/labels';
import { shiftState, shiftStateLabel } from './shiftUtils';
import s from './ShiftDetails.module.css';

interface Props {
  shift: CareShiftResponse;
  myId: number | null;
  registering: boolean;
  onRegister: () => void;
  onSwap: () => void;
  onClose?: () => void;
}

export function ShiftDetails({ shift, myId, registering, onRegister, onSwap, onClose }: Props) {
  const start = parseApiDate(shift.startTime);
  const end = parseApiDate(shift.endTime);
  const st = shiftState(shift);
  const iAmIn = shift.caregivers.some((c) => c.caregiverId === myId);
  const openSlots = Math.max(shift.capacity - shift.caregivers.length, 0);

  return (
    <div className={s.wrap}>
      <div className={`${s.hero} ${s[st]}`}>
        <div>
          <span className={s.kicker}>משמרת של החולה</span>
          <Link to={`/patients/${shift.patientId}`} className={s.code}>
            {shift.patientMedicalCode} <ExternalLink size={14} aria-hidden />
          </Link>
        </div>
        {onClose && (
          <button className={s.close} onClick={onClose} aria-label="סגירת פרטים"><X size={18} /></button>
        )}
      </div>

      <dl className={s.facts}>
        <div>
          <dt><CalendarDays size={16} aria-hidden /> תאריך</dt>
          <dd>{formatWeekday(start)}, {formatFullDate(start)}</dd>
        </div>
        <div>
          <dt><Clock size={16} aria-hidden /> שעות</dt>
          <dd><span className="num">{formatTime(start)} – {formatTime(end)}</span> · {formatDuration(start, end)}</dd>
        </div>
        <div>
          <dt><UserRound size={16} aria-hidden /> דרישת מין</dt>
          <dd>{genderRequirementLabel(shift.requiredGender)}</dd>
        </div>
      </dl>

      <section>
        <div className={s.slotsHead}>
          <h3>מי משובץ</h3>
          <Badge tone={st === 'full' ? 'indigo' : st === 'partial' ? 'brand' : 'amber'}>{shiftStateLabel[st]}</Badge>
        </div>
        <ul className={s.slots}>
          {shift.caregivers.map((c) => (
            <li key={c.caregiverId} className={s.filled}>
              <UserCheck size={16} aria-hidden />
              <span>{c.fullName}</span>
              {c.caregiverId === myId && <Badge tone="brand">אתה</Badge>}
            </li>
          ))}
          {Array.from({ length: openSlots }, (_, i) => (
            <li key={`open-${i}`} className={s.open}>
              <UserPlus size={16} aria-hidden /> מקום פנוי
            </li>
          ))}
        </ul>
      </section>

      {shift.tags.length > 0 && (
        <section>
          <h3 className={s.tagsTitle}>תגיות</h3>
          <div className={s.tags}>{shift.tags.map((t) => <Badge key={t} tone="neutral">{t}</Badge>)}</div>
        </section>
      )}

      <div className={s.actions}>
        {iAmIn ? (
          <Button icon={<ArrowLeftRight size={18} />} onClick={onSwap}>בקשת החלפה</Button>
        ) : (
          <Button icon={<UserPlus size={18} />} loading={registering} disabled={shift.isFull} onClick={onRegister}>
            {shift.isFull ? 'המשמרת מלאה' : 'שבץ אותי'}
          </Button>
        )}
        {!iAmIn && (
          <p className={s.hint}><Info size={15} aria-hidden /> כדי לבקש החלפה צריך להיות משובץ במשמרת. אחרי שתשתבץ תוכל להחליף אותה.</p>
        )}
      </div>
    </div>
  );
}
