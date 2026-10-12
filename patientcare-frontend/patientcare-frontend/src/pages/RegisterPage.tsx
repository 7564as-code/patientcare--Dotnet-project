import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { UserPlus } from 'lucide-react';
import { useAuth } from '@/auth/AuthContext';
import { getErrorMessage } from '@/api';
import { Button } from '@/components/ui/Button';
import { Gender } from '@/types/enums';
import s from './LoginPage.module.css';

export function RegisterPage() {
  const { isAuthenticated, register } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [gender, setGender] = useState<Gender>(Gender.Male);
  const [dob, setDob] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) return <Navigate to="/" replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await register({ fullName: fullName.trim(), email: email.trim(), password, caregiverGender: gender, dateOfBirth: dob });
      navigate('/', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className={s.page}>
      <aside className={s.hero} aria-hidden>
        <div className={s.heroInner}>
          <div className={s.mark}>
            <svg viewBox="0 0 32 32" width="26" height="26"><path d="M16 7v18M7 16h18" stroke="currentColor" strokeWidth="3.4" strokeLinecap="round" /></svg>
          </div>
          <h2>מצטרפים למעגל המשפחה ורואים את כל המשמרות במקום אחד.</h2>
        </div>
      </aside>
      <main className={s.formSide}>
        <form className={s.form} onSubmit={onSubmit}>
          <div>
            <h1>הרשמה</h1>
            <p className="muted">אחרי ההרשמה תוכלו ליצור חולה חדש או להצטרף למעגל עם קוד הזמנה.</p>
          </div>
          <div className="field">
            <label htmlFor="fn">שם מלא</label>
            <input id="fn" className="input" autoComplete="name" required value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="em">אימייל</label>
            <input id="em" className="input" type="email" dir="ltr" autoComplete="username" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="pw">סיסמה</label>
            <input id="pw" className="input" type="password" dir="ltr" autoComplete="new-password" minLength={6} required value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          <div className="form-grid two">
            <div className="field">
              <label htmlFor="g">מין</label>
              <select id="g" className="input" value={gender} onChange={(e) => setGender(Number(e.target.value) as Gender)}>
                <option value={Gender.Male}>זכר</option>
                <option value={Gender.Female}>נקבה</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="db">תאריך לידה</label>
              <input id="db" className="input" type="date" required max={new Date().toISOString().slice(0, 10)} value={dob} onChange={(e) => setDob(e.target.value)} />
            </div>
          </div>
          {error && <div className={s.error} role="alert">{error}</div>}
          <Button type="submit" loading={busy} disabled={!fullName || !email || !password || !dob} icon={<UserPlus size={18} />}>יצירת חשבון</Button>
          <p className={s.alt}>כבר יש לך חשבון? <Link to="/login">התחברות</Link></p>
        </form>
      </main>
    </div>
  );
}
