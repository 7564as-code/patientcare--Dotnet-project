import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, ArrowLeftRight, Check, Clock, Inbox, Send, X } from 'lucide-react';
import { getErrorMessage, shiftsApi, swapRequestsApi } from '@/api';
import { useAsync } from '@/hooks/useAsync';
import { useToast } from '@/components/ui/Toast';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/States';
import { formatDateTime, formatDayMonth, formatWeekday, parseApiDate, timeRange } from '@/utils/dates';
import { initials, swapStatusMeta } from '@/utils/labels';
import { SwapRequestStatus } from '@/types/enums';
import type { CareShiftResponse, SwapRequestResponse } from '@/types/dto';
import s from './SwapRequestsPage.module.css';

type Tab = 'incoming' | 'mine';

export function SwapRequestsPage() {
  const toast = useToast();
  const [tab, setTab] = useState<Tab>('incoming');
  const incoming = useAsync(swapRequestsApi.incoming);
  const mine = useAsync(swapRequestsApi.mine);
  const shifts = useAsync(shiftsApi.list);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [rejecting, setRejecting] = useState<SwapRequestResponse | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const shiftById = useMemo(() => new Map((shifts.data ?? []).map((x) => [x.id, x])), [shifts.data]);
  const current = tab === 'incoming' ? incoming : mine;

  async function approve(r: SwapRequestResponse) {
    setBusyId(r.id);
    try {
      await swapRequestsApi.approve(r.id);
      toast.success(`ההחלפה עם ${r.requestorName} בוצעה`);
      incoming.reload();
      mine.reload();
      shifts.reload();
    } catch (e) {
      toast.error(getErrorMessage(e));
      incoming.reload();
    } finally {
      setBusyId(null);
    }
  }

  async function cancel(r: SwapRequestResponse) {
    setBusyId(r.id);
    try {
      await swapRequestsApi.cancel(r.id);
      toast.success('הבקשה בוטלה');
      mine.reload();
      incoming.reload();
    } catch (e) {
      toast.error(getErrorMessage(e));
      mine.reload();
    } finally {
      setBusyId(null);
    }
  }

  async function reject() {
    if (!rejecting) return;
    const r = rejecting;
    setBusyId(r.id);
    try {
      await swapRequestsApi.reject(r.id, rejectReason);
      toast.success(`הבקשה של ${r.requestorName} נדחתה`);
      setRejecting(null);
      setRejectReason('');
      incoming.reload();
      mine.reload();
    } catch (e) {
      toast.error(getErrorMessage(e));
      incoming.reload();
    } finally {
      setBusyId(null);
    }
  }

  const list = current.data ?? [];

  return (
    <div className="page">
      <PageHeader title="בקשות החלפה" subtitle="החלפות משמרות בין בני המשפחה." />

      <div className={s.tabs} role="tablist">
        <button role="tab" aria-selected={tab === 'incoming'} className={tab === 'incoming' ? s.tabActive : ''} onClick={() => setTab('incoming')}>
          ממתינות לאישורי
          {(incoming.data?.length ?? 0) > 0 && <span className={s.count}>{incoming.data?.length}</span>}
        </button>
        <button role="tab" aria-selected={tab === 'mine'} className={tab === 'mine' ? s.tabActive : ''} onClick={() => setTab('mine')}>
          הבקשות ששלחתי
        </button>
      </div>

      {current.loading && !current.data && <Spinner />}
      {current.error && <ErrorState message={current.error} onRetry={current.reload} />}
      {current.data && list.length === 0 && (
        <Card>
          <EmptyState
            icon={tab === 'incoming' ? <Inbox size={22} /> : <Send size={22} />}
            title={tab === 'incoming' ? 'אין בקשות שממתינות לאישורך' : 'עוד לא שלחת בקשות החלפה'}
            hint={tab === 'incoming'
              ? 'כשבן משפחה יבקש להחליף איתך משמרת, הבקשה תופיע כאן ותוכל לאשר אותה.'
              : 'כדי לבקש החלפה, פתח משמרת שאתה משובץ בה בלוח המשמרות.'}
            action={<Link to="/shifts" className={s.link}>ללוח המשמרות</Link>}
          />
        </Card>
      )}

      <ul className={s.list}>
        {list.map((r) => {
          const meta = swapStatusMeta[r.status];
          const own = shiftById.get(r.careShiftId);
          const target = shiftById.get(r.targetCareShiftId);
          return (
            <li key={r.id}>
              <Card className={s.item}>
                <div className={s.swapRow}>
                  <Side name={r.requestorName} role="נותן/ת" shift={own} />
                  <span className={s.swap} aria-hidden><ArrowLeftRight size={18} /></span>
                  <Side name={r.targetCaregiverName ?? 'לא ידוע'} role="נותן/ת" shift={target} />
                </div>

                {r.reason && <p className={s.reason}>“{r.reason}”</p>}
                {r.status === SwapRequestStatus.Rejected && (
                  <p className={s.rejected}>
                    <strong>נדחתה{r.targetCaregiverName ? ` על ידי ${r.targetCaregiverName}` : ''}.</strong>{' '}
                    {r.responseReason ? `סיבה: ${r.responseReason}` : 'לא צוינה סיבה.'}
                  </p>
                )}

                <div className={s.footer}>
                  <div className={s.meta}>
                    <Badge tone={meta.tone}>{meta.label}</Badge>
                    <span className="muted"><Clock size={14} aria-hidden /> נשלחה ב־<span className="num">{formatDateTime(parseApiDate(r.requestedAt))}</span></span>
                    <Link to={`/shifts?shift=${r.careShiftId}`} className={s.link}>לצפייה בלוח <ArrowLeft size={14} /></Link>
                  </div>
                  {tab === 'incoming' && r.status === SwapRequestStatus.Pending && (
                    <div className={s.actions}>
                      <Button variant="secondary" icon={<X size={18} />} disabled={busyId === r.id} onClick={() => { setRejectReason(''); setRejecting(r); }}>דחייה</Button>
                      <Button icon={<Check size={18} />} loading={busyId === r.id} onClick={() => approve(r)}>אישור החלפה</Button>
                    </div>
                  )}
                  {tab === 'mine' && r.status === SwapRequestStatus.Pending && (
                    <div className={s.actions}>
                      <span className={s.waiting}>ממתין לאישור של {r.targetCaregiverName ?? 'בן המשפחה במשמרת היעד'}</span>
                      <Button variant="ghost" icon={<X size={18} />} loading={busyId === r.id} onClick={() => cancel(r)}>ביטול הבקשה</Button>
                    </div>
                  )}
                </div>
              </Card>
            </li>
          );
        })}
      </ul>

      <Modal
        open={!!rejecting}
        onClose={() => setRejecting(null)}
        title="דחיית בקשת החלפה"
        footer={
          <>
            <Button variant="danger" loading={rejecting != null && busyId === rejecting.id} onClick={reject}>דחיית הבקשה</Button>
            <Button variant="ghost" onClick={() => setRejecting(null)}>ביטול</Button>
          </>
        }
      >
        <p>
          לדחות את הבקשה של <strong>{rejecting?.requestorName}</strong>? אפשר לכתוב סיבה, אבל זה לא חובה.
        </p>
        <div className="field">
          <label htmlFor="reject-reason">סיבה (לא חובה)</label>
          <textarea
            id="reject-reason"
            className="input"
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            placeholder="למשל: לא מתאים לי באותו יום"
            maxLength={500}
          />
        </div>
      </Modal>
    </div>
  );
}

function Side({ name, role, shift }: { name: string; role: string; shift?: CareShiftResponse }) {
  return (
    <div className={s.side}>
      <div className={s.person}>
        <span className={s.avatar} aria-hidden>{initials(name)}</span>
        <div>
          <strong>{name}</strong>
          <span className="muted">{role} את המשמרת</span>
        </div>
      </div>
      {shift ? (
        <div className={s.shiftBox}>
          <strong>{formatWeekday(parseApiDate(shift.startTime))} {formatDayMonth(parseApiDate(shift.startTime))}</strong>
          <span>{timeRange(parseApiDate(shift.startTime), parseApiDate(shift.endTime))}</span>
          <span className="muted">{shift.patientMedicalCode}</span>
        </div>
      ) : (
        <div className={s.shiftBox}><span className="muted">פרטי המשמרת אינם זמינים</span></div>
      )}
    </div>
  );
}
