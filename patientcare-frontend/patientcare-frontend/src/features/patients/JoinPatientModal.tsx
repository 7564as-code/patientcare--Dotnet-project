import { useState, type FormEvent } from 'react';
import { getErrorMessage, patientsApi } from '@/api';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import type { PatientResponse } from '@/types/dto';

interface Props {
  open: boolean;
  onClose: () => void;
  onJoined: (p: PatientResponse) => void;
}

export function JoinPatientModal({ open, onClose, onJoined }: Props) {
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const p = await patientsApi.join(code.trim());
      setCode('');
      onJoined(p);
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
      title="הצטרפות למעגל של חולה"
      footer={
        <>
          <Button type="submit" form="join-form" loading={busy} disabled={!code.trim()}>הצטרפות</Button>
          <Button variant="ghost" onClick={onClose}>ביטול</Button>
        </>
      }
    >
      <form id="join-form" className="form-grid" onSubmit={submit}>
        <p className="muted">הזינו את קוד ההזמנה שקיבלתם ממנהל החולה כדי לראות את המשמרות שלו ולהשתבץ אליהן.</p>
        <div className="field">
          <label htmlFor="invite">קוד הזמנה</label>
          <input id="invite" className="input" dir="ltr" autoComplete="off" autoCapitalize="characters" maxLength={16}
            style={{ letterSpacing: '0.18em', textTransform: 'uppercase', fontWeight: 600, fontSize: '1.1rem' }}
            value={code} onChange={(e) => setCode(e.target.value)} placeholder="ABCD2345" />
        </div>
        {error && <div role="alert" style={{ color: 'var(--rose)', fontSize: '0.9rem' }}>{error}</div>}
      </form>
    </Modal>
  );
}
