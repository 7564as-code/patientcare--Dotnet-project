import { Link } from 'react-router-dom';
import { ArrowLeftRight, CalendarCheck, CalendarDays, ChevronLeft, UserPlus, Users } from 'lucide-react';
import { patientsApi, shiftsApi, swapRequestsApi } from '@/api';
import { useAuth } from '@/auth/AuthContext';
import { useAsync } from '@/hooks/useAsync';
import { Card } from '@/components/ui/Card';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/States';
import { Occupancy } from '@/features/shifts/Occupancy';
import { shiftState } from '@/features/shifts/shiftUtils';
import { formatFullDate, formatTime, formatWeekday, formatDayMonth, parseApiDate, relativeDay } from '@/utils/dates';
import { formatDateTime } from '@/utils/dates';
import s from './DashboardPage.module.css';

export function DashboardPage() {
  const { user } = useAuth();
  const patients = useAsync(patientsApi.list);
  const shifts = useAsync(shiftsApi.list);
  const pending = useAsync(() => swapRequestsApi.incoming().catch(() => []), []);

  const now = new Date();

  const all = shifts.data ?? [];
  const upcoming = all
    .filter((x) => parseApiDate(x.endTime) >= now)
    .sort((a, b) => parseApiDate(a.startTime).getTime() - parseApiDate(b.startTime).getTime());
  const openSlots = upcoming.filter((x) => !x.isFull);
  const myUpcoming = upcoming.filter((x) => x.caregivers.some((c) => c.caregiverId === user?.caregiverId));
  const firstName = user?.fullName?.split(' ')[0] ?? '';

  const loading = patients.loading || shifts.loading;
  const error = patients.error ?? shifts.error;

  return (
    <div className="page">
      <header className={s.hello}>
        <p className="muted">{formatFullDate(now)}</p>
        <h1>שלום {firstName}</h1>
      </header>

      {loading && <Spinner />}
      {error && <ErrorState message={error} onRetry={() => { patients.reload(); shifts.reload(); }} />}

      {!loading && !error && (
        <>
          <div className={s.stats}>
            <Stat to="/patients" icon={<Users size={20} />} value={patients.data?.length ?? 0} label="חולים במעגלים שלי" />
            <Stat to="/shifts" icon={<CalendarDays size={20} />} value={myUpcoming.length} label="המשמרות הקרובות שלי" />
            <Stat to="/shifts" icon={<CalendarCheck size={20} />} value={openSlots.length} label="משמרות עם מקום פנוי" tone="amber" />
            <Stat to="/swaps" icon={<ArrowLeftRight size={20} />} value={pending.data?.length ?? 0} label="בקשות החלפה לאישורך" tone="indigo" />
          </div>

          <div className={s.cols}>
            <section>
              <div className={s.sectionHead}>
                <h2>המשמרות הקרובות במעגלים שלי</h2>
                <Link to="/shifts" className={s.more}>ללוח המלא <ChevronLeft size={16} /></Link>
              </div>
              {upcoming.length === 0 ? (
                <Card><EmptyState icon={<CalendarDays size={22} />} title="אין משמרות קרובות" hint="כשייווצרו משמרות חדשות הן יופיעו כאן." /></Card>
              ) : (
                <ul className={s.list}>
                  {upcoming.slice(0, 6).map((sh) => {
                    const start = parseApiDate(sh.startTime);
                    const st = shiftState(sh);
                    return (
                      <li key={sh.id}>
                        <Link to={`/shifts?shift=${sh.id}`} className={`${s.row} ${s[st]}`}>
                          <div className={s.date}>
                            <strong>{relativeDay(start) ?? formatWeekday(start)}</strong>
                            <span>{formatDayMonth(start)}</span>
                          </div>
                          <div className={s.info}>
                            <span className="num">{formatTime(start)} – {formatTime(parseApiDate(sh.endTime))}</span>
                            <span className={s.patient}>{sh.patientMedicalCode}</span>
                          </div>
                          <Occupancy shift={sh} showText={false} />
                        </Link>
                      </li>
                    );
                  })}
                </ul>
              )}
            </section>

            <aside>
              <div className={s.sectionHead}><h2>ממתין לאישורך</h2></div>
              <Card className={s.pendingCard}>
                {pending.data && pending.data.length > 0 ? (
                  <ul className={s.pendingList}>
                    {pending.data.slice(0, 4).map((r) => (
                      <li key={r.id}>
                        <strong>{r.requestorName}</strong>
                        <span className="muted">מבקש/ת להחליף איתך · {formatDateTime(parseApiDate(r.requestedAt))}</span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="muted">אין בקשות החלפה שממתינות לאישורך.</p>
                )}
                <Link to="/swaps" className={s.more}>לכל הבקשות <ChevronLeft size={16} /></Link>
              </Card>

              <Link to="/patients" className={s.quick}><UserPlus size={18} /> החולים שלי</Link>
            </aside>
          </div>
        </>
      )}
    </div>
  );
}

function Stat({ to, icon, value, label, tone = 'brand' }: { to: string; icon: React.ReactNode; value: number; label: string; tone?: 'brand' | 'amber' | 'indigo' }) {
  return (
    <Link to={to} className={`${s.stat} ${s[`tone_${tone}`]}`}>
      <span className={s.statIcon}>{icon}</span>
      <span className={`${s.statValue} num`}>{value}</span>
      <span className={s.statLabel}>{label}</span>
    </Link>
  );
}

