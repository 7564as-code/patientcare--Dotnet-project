import { useState, type FormEvent } from 'react';
import { patientsApi, getErrorMessage } from '@/api';
import { Gender } from '@/types/enums';
import type { PatientResponse } from '@/types/dto';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated: (p: PatientResponse) => void;
}

export function PatientFormModal({ open, onClose, onCreated }: Props) {
  const [medicalCode, setMedicalCode] = useState('');
  const [gender, setGender] = useState<Gender>(Gender.Male);
  const [dob, setDob] = useState('');
  const [department, setDepartment] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const p = await patientsApi.create({ medicalCode: medicalCode.trim(), patientGender: gender, dateOfBirth: dob, department: department.trim() });
      setMedicalCode(''); setDob(''); setDepartment(''); setGender(Gender.Male);
      onCreated(p);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const valid = medicalCode.trim() && dob && department.trim();

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="חולה חדש"
      footer={
        <>
          <Button type="submit" form="patient-form" loading={busy} disabled={!valid}>שמירת חולה</Button>
          <Button variant="ghost" onClick={onClose}>ביטול</Button>
        </>
      }
    >
      <form id="patient-form" className="form-grid" onSubmit={submit}>
        <div className="field">
          <label htmlFor="pc">שם החולה</label>
          <input id="pc" className="input" value={medicalCode} onChange={(e) => setMedicalCode(e.target.value)} maxLength={50} placeholder="למשל: אמא, סבא יוסף" required />
        </div>
        <div className="form-grid two">
          <div className="field">
            <label htmlFor="pg">מין</label>
            <select id="pg" className="input" value={gender} onChange={(e) => setGender(Number(e.target.value) as Gender)}>
              <option value={Gender.Male}>זכר</option>
              <option value={Gender.Female}>נקבה</option>
            </select>
          </div>
          <div className="field">
            <label htmlFor="pd">תאריך לידה</label>
            <input id="pd" className="input" type="date" max={new Date().toISOString().slice(0, 10)} value={dob} onChange={(e) => setDob(e.target.value)} required />
          </div>
        </div>
        <div className="field">
          <label htmlFor="pdep">מקום</label>
          <input id="pdep" className="input" value={department} onChange={(e) => setDepartment(e.target.value)} maxLength={100} placeholder="למשל: בית, בית חולים איכילוב" required />
          <span className="field-hint">אתה תהיה מנהל החולה, ותוכל להזמין בני משפחה עם קוד הזמנה.</span>
        </div>
        {error && <div role="alert" style={{ color: 'var(--rose)', fontSize: '0.9rem' }}>{error}</div>}
      </form>
    </Modal>
  );
}
