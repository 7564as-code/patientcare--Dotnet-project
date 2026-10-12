import { useEffect, useState, type FormEvent } from 'react';
import { getErrorMessage, patientsApi, shiftsApi } from '@/api';
import { useAsync } from '@/hooks/useAsync';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { GenderRequirement } from '@/types/enums';
import type { CareShiftResponse } from '@/types/dto';

interface Props {
  open: boolean;
  /** כשמועבר – החולה נעול (פתיחה מדף החולה) */
  patientId?: number;
  onClose: () => void;
  onCreated: (shift: CareShiftResponse) => void;
}

const toLocalInput = (d: Date) => {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
};

function defaults() {
  const start = new Date();
  start.setDate(start.getDate() + 1);
  start.setHours(8, 0, 0, 0);
  const end = new Date(start);
  end.setHours(16);
  return { start: toLocalInput(start), end: toLocalInput(end) };
}

export function ShiftFormModal({ open, patientId, onClose, onCreated }: Props) {
  const patients = useAsync(patientsApi.list);
  const [pid, setPid] = useState<number | ''>(patientId ?? '');
  const [start, setStart] = useState(defaults().start);
  const [end, setEnd] = useState(defaults().end);
  const [gender, setGender] = useState<GenderRequirement>(GenderRequirement.Any);
  const [capacity, setCapacity] = useState(1);
  const [tags, setTags] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      const d = defaults();
      setStart(d.start); setEnd(d.end); setError(null);
      setPid(patientId ?? '');
    }
  }, [open, patientId]);

  const endBeforeStart = !!start && !!end && new Date(end) <= new Date(start);
  const valid = pid !== '' && start && end && !endBeforeStart && capacity >= 1;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (pid === '') return;
    setBusy(true);
    setError(null);
    try {
      const list = tags.split(',').map((t) => t.trim()).filter(Boolean);
      const created = await shiftsApi.create({
        patientId: pid,
        startTime: start,
        endTime: end,
        requiredGender: gender,
        capacity,
        tags: list.length ? list : undefined,
      });
      setTags(''); setCapacity(1); setGender(GenderRequirement.Any);
      onCreated(created);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="משמרת חדשה"
      footer={
        <>
          <Button type="submit" form="shift-form" loading={busy} disabled={!valid}>יצירת משמרת</Button>
          <Button variant="ghost" onClick={onClose}>ביטול</Button>
        </>
      }
    >
      <form id="shift-form" className="form-grid" onSubmit={submit}>
        <div className="field">
          <label htmlFor="sf-patient">חולה</label>
          <select id="sf-patient" className="input" value={pid} disabled={patientId !== undefined} onChange={(e) => setPid(e.target.value ? Number(e.target.value) : '')}>
            <option value="">בחר חולה…</option>
            {(patients.data ?? []).filter((p) => p.isManager).map((p) => (
              <option key={p.id} value={p.id}>{p.medicalCode} · {p.department}</option>
            ))}
          </select>
        </div>
        <div className="form-grid two">
          <div className="field">
            <label htmlFor="sf-start">התחלה</label>
            <input id="sf-start" className="input" type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} required />
          </div>
          <div className="field">
            <label htmlFor="sf-end">סיום</label>
            <input id="sf-end" className="input" type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} required />
          </div>
        </div>
        {endBeforeStart && <div role="alert" style={{ color: 'var(--rose)', fontSize: '0.88rem' }}>שעת הסיום חייבת להיות אחרי שעת ההתחלה.</div>}
        <div className="form-grid two">
          <div className="field">
            <label htmlFor="sf-gender">דרישת מין</label>
            <select id="sf-gender" className="input" value={gender} onChange={(e) => setGender(Number(e.target.value) as GenderRequirement)}>
              <option value={GenderRequirement.Any}>ללא הגבלה</option>
              <option value={GenderRequirement.Male}>גברים בלבד</option>
              <option value={GenderRequirement.Female}>נשים בלבד</option>
            </select>
          </div>
          <div className="field">
            <label htmlFor="sf-cap">כמה בני משפחה נדרשים</label>
            <input id="sf-cap" className="input" type="number" min={1} max={20} dir="ltr" value={capacity} onChange={(e) => setCapacity(Number(e.target.value))} />
          </div>
        </div>
        <div className="field">
          <label htmlFor="sf-tags">תגיות (לא חובה)</label>
          <input id="sf-tags" className="input" value={tags} onChange={(e) => setTags(e.target.value)} placeholder="מופרדות בפסיק, למשל: ניטור, לילה" />
        </div>
        {error && <div role="alert" style={{ color: 'var(--rose)', fontSize: '0.9rem' }}>{error}</div>}
      </form>
    </Modal>
  );
}
