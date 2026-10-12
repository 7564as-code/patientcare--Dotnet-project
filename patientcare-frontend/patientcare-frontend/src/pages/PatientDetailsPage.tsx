import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowRight, Building2, CalendarDays, Cake, Copy, Crown, Hourglass, KeyRound, Mars, Plus, RefreshCw, UserCog, Users, Venus } from 'lucide-react';
import { getErrorMessage, patientsApi, shiftsApi } from '@/api';
import { useAuth } from '@/auth/AuthContext';
import { useAsync } from '@/hooks/useAsync';
import { useToast } from '@/components/ui/Toast';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/States';
import { Occupancy } from '@/features/shifts/Occupancy';
import { ShiftFormModal } from '@/features/shifts/ShiftFormModal';
import { shiftState, shiftStateLabel } from '@/features/shifts/shiftUtils';
import { ageCategoryLabel, genderLabel, genderRequirementLabel, initials } from '@/utils/labels';
import { formatDayMonth, formatFullDate, formatTime, formatWeekday, parseApiDate } from '@/utils/dates';
import { Gender } from '@/types/enums';
import s from './PatientDetailsPage.module.css';

export function PatientDetailsPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const toast = useToast();
  const patient = useAsync(() => patientsApi.get(id), [id]);
  const shifts = useAsync(() => shiftsApi.list(), []);
  const members = useAsync(() => patientsApi.members(id), [id]);

  const [createOpen, setCreateOpen] = useState(false);
  const [newManagerId, setNewManagerId] = useState('');
  const [busy, setBusy] = useState(false);
  const [code, setCode] = useState<string | null>(null);

  if (patient.loading) return <div className="page"><Spinner /></div>;
  if (patient.error || !patient.data) return <div className="page"><ErrorState message={patient.error ?? 'החולה לא נמצא'} onRetry={patient.reload} /></div>;

  const p = patient.data;
  const isManager = p.isManager;
  const mine = (shifts.data ?? [])
    .filter((x) => x.patientId === p.id)
    .sort((a, b) => parseApiDate(a.startTime).getTime() - parseApiDate(b.startTime).getTime());
  const others = (members.data ?? []).filter((m) => !m.isManager);

  async function run<T>(fn: () => Promise<T>, ok: string, then?: (r: T) => void) {
    setBusy(true);
    try {
      const r = await fn();
      toast.success(ok);
      then?.(r);
    } catch (e) {
      toast.error(getErrorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  const showCode = () => run(() => patientsApi.inviteCode(p.id), 'קוד ההזמנה מוכן', (r) => setCode(r.inviteCode));
  const resetCode = () => run(() => patientsApi.resetInviteCode(p.id), 'נוצר קוד חדש. הקוד הקודם בוטל.', (r) => setCode(r.inviteCode));
  const changeManager = () =>
    run(() => patientsApi.changeManager(p.id, { newManagerCaregiverId: Number(newManagerId) }), 'ניהול החולה הועבר', () => {
      setNewManagerId('');
      patient.reload();
      members.reload();
    });

  async function copyCode() {
    if (!code) return;
    try {
      await navigator.clipboard.writeText(code);
      toast.success('הקוד הועתק');
    } catch {
      toast.error('לא הצלחנו להעתיק. סמנו את הקוד והעתיקו ידנית.');
    }
  }

  const GenderIcon = p.patientGender === Gender.Male ? Mars : Venus;

  return (
    <div className="page">
      <Link to="/patients" className={s.back}><ArrowRight size={16} /> חזרה לחולים</Link>

      <header className={s.hero}>
        <div>
          <p className="muted">חולה</p>
          <h1 className={s.code}>{p.medicalCode}</h1>
        </div>
        <div className={s.heroBadges}>
          {isManager && <Badge tone="amber" icon={<Crown size={13} />}>אתה המנהל</Badge>}
          <Badge tone="brand">{ageCategoryLabel(p.ageCategory)}</Badge>
        </div>
      </header>

      <div className={s.facts}>
        <Fact icon={<Building2 size={18} />} label="מקום" value={p.department} />
        <Fact icon={<GenderIcon size={18} />} label="מין" value={genderLabel(p.patientGender)} />
        <Fact icon={<Cake size={18} />} label="תאריך לידה" value={formatFullDate(parseApiDate(p.dateOfBirth))} />
        <Fact icon={<Hourglass size={18} />} label="גיל" value={`${p.age}`} />
      </div>

      <div className={s.cols}>
        <section>
          <div className={s.sectionHead}>
            <h2>משמרות</h2>
            {isManager && <Button size="sm" icon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>משמרת חדשה</Button>}
          </div>
          {shifts.loading && <Spinner />}
          {shifts.error && <ErrorState message={shifts.error} onRetry={shifts.reload} />}
          {!shifts.loading && !shifts.error && mine.length === 0 && (
            <Card>
              <EmptyState
                icon={<CalendarDays size={22} />}
                title="אין משמרות לחולה"
                hint={isManager ? 'צרו את המשמרת הראשונה, ובני המשפחה יוכלו להשתבץ אליה.' : 'המנהל עדיין לא יצר משמרות לחולה הזה.'}
              />
            </Card>
          )}
          <ul className={s.list}>
            {mine.map((sh) => {
              const st = shiftState(sh);
              const start = parseApiDate(sh.startTime);
              const names = sh.caregivers.map((c) => (c.caregiverId === user?.caregiverId ? 'אתה' : c.fullName));
              return (
                <li key={sh.id}>
                  <Link to={`/shifts?shift=${sh.id}`} className={`${s.shift} ${s[st]}`}>
                    <div className={s.when}>
                      <strong>{formatWeekday(start)}</strong>
                      <span>{formatDayMonth(start)}</span>
                    </div>
                    <div className={s.what}>
                      <span className="num">{formatTime(start)} – {formatTime(parseApiDate(sh.endTime))}</span>
                      <Occupancy shift={sh} />
                      {names.length > 0 && <span className={s.names}>{names.join(', ')}</span>}
                    </div>
                    <div className={s.tags}>
                      <Badge tone={st === 'full' ? 'indigo' : st === 'partial' ? 'brand' : 'amber'}>{shiftStateLabel[st]}</Badge>
                      <Badge>{genderRequirementLabel(sh.requiredGender)}</Badge>
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>

        <aside className={s.side}>
          <div>
            <h2 className={s.h}>מעגל המשפחה</h2>
            <Card className={s.circle}>
              {members.loading && <Spinner />}
              {members.error && <ErrorState message={members.error} onRetry={members.reload} />}
              <ul className={s.members}>
                {members.data?.map((m) => (
                  <li key={m.caregiverId}>
                    <span className={s.avatar} aria-hidden>{initials(m.fullName)}</span>
                    <span className={s.memberName}>
                      {m.fullName}
                      {m.caregiverId === user?.caregiverId && <span className="muted"> (אתה)</span>}
                    </span>
                    {m.isManager && <Badge tone="amber" icon={<Crown size={12} />}>מנהל</Badge>}
                  </li>
                ))}
              </ul>
            </Card>
          </div>

          {isManager && (
            <Card className={s.manage}>
              <div className={s.manageHead}><KeyRound size={18} /> הזמנת בני משפחה</div>
              <p className="muted">שלחו את הקוד לבני המשפחה. הם יזינו אותו ב"הצטרפות עם קוד" וייכנסו למעגל.</p>
              {code ? (
                <>
                  <div className={s.codeBox} dir="ltr">{code}</div>
                  <div className={s.row}>
                    <Button variant="secondary" size="sm" icon={<Copy size={15} />} onClick={copyCode}>העתקה</Button>
                    <Button variant="ghost" size="sm" icon={<RefreshCw size={15} />} loading={busy} onClick={resetCode}>קוד חדש</Button>
                  </div>
                </>
              ) : (
                <Button variant="secondary" loading={busy} onClick={showCode}>הצגת קוד הזמנה</Button>
              )}
            </Card>
          )}

          {isManager && (
            <Card className={s.manage}>
              <div className={s.manageHead}><UserCog size={18} /> העברת ניהול</div>
              <p className="muted">אפשר להעביר את הניהול לבן משפחה שכבר הצטרף למעגל. אתה תישאר חבר במעגל.</p>
              {others.length === 0 ? (
                <p className={s.note}><Users size={15} aria-hidden /> אין עדיין בני משפחה נוספים במעגל.</p>
              ) : (
                <>
                  <div className="field">
                    <label htmlFor="mgr">מנהל חדש</label>
                    <select id="mgr" className="input" value={newManagerId} onChange={(e) => setNewManagerId(e.target.value)}>
                      <option value="">בחר בן משפחה…</option>
                      {others.map((m) => <option key={m.caregiverId} value={m.caregiverId}>{m.fullName}</option>)}
                    </select>
                  </div>
                  <Button variant="secondary" loading={busy} disabled={!newManagerId} onClick={changeManager}>העבר ניהול</Button>
                </>
              )}
            </Card>
          )}
        </aside>
      </div>

      <ShiftFormModal
        open={createOpen}
        patientId={p.id}
        onClose={() => setCreateOpen(false)}
        onCreated={() => {
          setCreateOpen(false);
          toast.success('המשמרת נוצרה');
          shifts.reload();
        }}
      />
    </div>
  );
}

function Fact({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <Card className={s.fact}>
      <span className={s.factIcon}>{icon}</span>
      <div>
        <div className="muted" style={{ fontSize: '0.8rem' }}>{label}</div>
        <div style={{ fontWeight: 500 }}>{value}</div>
      </div>
    </Card>
  );
}
