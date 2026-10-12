import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { CalendarDays, ChevronLeft, ChevronRight, Plus, RefreshCw } from 'lucide-react';
import { getErrorMessage, patientsApi, shiftsApi } from '@/api';
import { useAuth } from '@/auth/AuthContext';
import { useAsync } from '@/hooks/useAsync';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { useToast } from '@/components/ui/Toast';
import { PageHeader } from '@/components/ui/PageHeader';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Modal } from '@/components/ui/Modal';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/States';
import { WeeklyCalendar } from '@/features/shifts/WeeklyCalendar';
import { DayAgenda } from '@/features/shifts/DayAgenda';
import { ShiftDetails } from '@/features/shifts/ShiftDetails';
import { ShiftFormModal } from '@/features/shifts/ShiftFormModal';
import { SwapDialog } from '@/features/shifts/SwapDialog';
import { addDays, formatWeekRange, isSameDay, parseApiDate, startOfDay, startOfWeek } from '@/utils/dates';
import type { CareShiftResponse } from '@/types/dto';
import s from './ShiftsPage.module.css';

export function ShiftsPage() {
  const toast = useToast();
  const { user } = useAuth();
  const myId = user?.caregiverId ?? null;
  const patients = useAsync(patientsApi.list);
  const canCreate = (patients.data ?? []).some((p) => p.isManager);
  const [params, setParams] = useSearchParams();
  const { data, loading, error, reload, setData } = useAsync(shiftsApi.list);
  const wide = useMediaQuery('(min-width: 1520px)');
  const isMobile = useMediaQuery('(max-width: 900px)');

  const [now, setNow] = useState(() => new Date());
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date()));
  const [day, setDay] = useState(() => startOfDay(new Date()));
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [swapOpen, setSwapOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [registering, setRegistering] = useState(false);

  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(t);
  }, []);

  const shifts = useMemo(() => data ?? [], [data]);
  const selected = shifts.find((x) => x.id === selectedId) ?? null;

  // קישור עמוק: /shifts?shift=ID קופץ לשבוע של המשמרת ובוחר אותה (פעם אחת, אחרי הטעינה).
  const deepLinked = useRef(false);
  useEffect(() => {
    if (deepLinked.current || !data) return;
    deepLinked.current = true;
    const id = Number(params.get('shift'));
    const target = data.find((x) => x.id === id);
    if (target) {
      const st = parseApiDate(target.startTime);
      setWeekStart(startOfWeek(st));
      setDay(startOfDay(st));
      setSelectedId(target.id);
    }
  }, [data, params]);

  const select = useCallback((sh: CareShiftResponse) => {
    setSelectedId(sh.id);
    setParams({ shift: String(sh.id) }, { replace: true });
  }, [setParams]);

  const closeDetails = useCallback(() => {
    setSelectedId(null);
    setParams({}, { replace: true });
  }, [setParams]);

  const weekEnd = addDays(weekStart, 7);
  const weekShifts = useMemo(
    () => shifts.filter((x) => parseApiDate(x.startTime) < weekEnd && parseApiDate(x.endTime) > weekStart),
    [shifts, weekStart, weekEnd],
  );

  function goToWeek(next: Date) {
    setWeekStart(next);
    // במובייל: שומרים את אותו יום-בשבוע; אם זה השבוע הנוכחי — היום.
    const today = startOfDay(now);
    setDay(isSameDay(startOfWeek(today), next) ? today : addDays(next, day.getDay()));
  }
  const goToday = () => { goToWeek(startOfWeek(new Date())); setDay(startOfDay(new Date())); };

  async function register() {
    if (!selected) return;
    setRegistering(true);
    try {
      const updated = await shiftsApi.register(selected.id);
      setData(shifts.map((x) => (x.id === updated.id ? updated : x)));
      toast.success('שובצת למשמרת');
    } catch (e) {
      toast.error(getErrorMessage(e));
      reload(); // ייתכן שהמשמרת השתנתה (409) — מרעננים
    } finally {
      setRegistering(false);
    }
  }

  const details = selected && (
    <ShiftDetails
      shift={selected}
      myId={myId}
      registering={registering}
      onRegister={register}
      onSwap={() => setSwapOpen(true)}
      onClose={wide ? closeDetails : undefined}
    />
  );


  return (
    <div className="page">
      <PageHeader
        title="לוח משמרות"
        subtitle={formatWeekRange(weekStart)}
        actions={
          <>
            <div className={s.nav} role="group" aria-label="ניווט שבועות">
              <button onClick={() => goToWeek(addDays(weekStart, -7))} aria-label="שבוע קודם"><ChevronRight size={20} /></button>
              <button className={s.todayBtn} onClick={goToday}>השבוע</button>
              <button onClick={() => goToWeek(addDays(weekStart, 7))} aria-label="שבוע הבא"><ChevronLeft size={20} /></button>
            </div>
            <Button variant="secondary" icon={<RefreshCw size={16} />} onClick={reload} aria-label="רענון" />
            {canCreate && <Button icon={<Plus size={18} />} onClick={() => setCreateOpen(true)}>משמרת חדשה</Button>}
          </>
        }
      />

      <ul className={s.legend} aria-label="מקרא">
        <li><i className={s.kFull} /> מלאה</li>
        <li><i className={s.kPartial} /> יש מקום פנוי</li>
        <li><i className={s.kEmpty} /> אין בן משפחה משובץ</li>
      </ul>

      {loading && !data && <Spinner label="טוען משמרות…" />}
      {error && <ErrorState message={error} onRetry={reload} />}

      {data && (
        <div className={`${s.layout} ${wide && selected ? s.withPanel : ''}`}>
          <div className={s.calWrap}>
            {shifts.length === 0 ? (
              <Card><EmptyState icon={<CalendarDays size={22} />} title="עדיין אין משמרות" hint="מנהל החולה יוצר משמרות, והן יופיעו כאן בלוח השבועי. אם עוד אין לך חולה, צור אחד או הצטרף עם קוד הזמנה." /></Card>
            ) : isMobile ? (
              <DayAgenda weekStart={weekStart} shifts={weekShifts} day={day} onDayChange={setDay} selectedId={selectedId} onSelect={select} now={now} myId={myId} />
            ) : (
              <>
                <WeeklyCalendar weekStart={weekStart} shifts={weekShifts} selectedId={selectedId} onSelect={select} now={now} myId={myId} />
                {weekShifts.length === 0 && <p className={s.emptyWeek}>אין משמרות בשבוע הזה. נסה לנווט לשבוע אחר.</p>}
              </>
            )}
          </div>

          {wide && selected && <aside className={s.panel} aria-label="פרטי משמרת"><Card className={s.panelCard}>{details}</Card></aside>}
        </div>
      )}

      <ShiftFormModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(sh) => {
          setCreateOpen(false);
          toast.success('המשמרת נוצרה');
          const st = parseApiDate(sh.startTime);
          setWeekStart(startOfWeek(st));
          setDay(startOfDay(st));
          setData([...shifts, sh]);
          select(sh);
        }}
      />

      {!wide && (
        <Modal open={!!selected} onClose={closeDetails} title="פרטי משמרת">
          {details}
        </Modal>
      )}

      {selected && (
        <SwapDialog
          open={swapOpen}
          ownShift={selected}
          myId={myId}
          allShifts={shifts}
          onClose={() => setSwapOpen(false)}
          onCreated={() => {
            setSwapOpen(false);
            toast.success('בקשת ההחלפה נשלחה לבן המשפחה במשמרת היעד');
          }}
        />
      )}
    </div>
  );
}
