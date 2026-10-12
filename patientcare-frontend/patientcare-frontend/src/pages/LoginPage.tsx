import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Eye, EyeOff, LogIn } from 'lucide-react';
import { useAuth } from '@/auth/AuthContext';
import { getErrorMessage } from '@/api';
import { Button } from '@/components/ui/Button';
import s from './LoginPage.module.css';

type Kind = 'full' | 'part' | 'empty';
const MOTIF: [number, number, Kind][][] = [
  [[6, 40, 'full'], [56, 34, 'part']],
  [[6, 28, 'part'], [40, 46, 'full']],
  [[20, 48, 'part']],
  [[6, 30, 'empty'], [44, 40, 'part']],
  [[10, 38, 'full'], [56, 30, 'empty']],
];

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) return <Navigate to={from} replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
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
          <h2>כל המשפחה סביב החולה: משמרות מסודרות, בלי בלבול.</h2>
          <div className={s.motif}>
            {MOTIF.map((col, i) => (
              <div key={i} className={s.motifCol}>
                {col.map(([top, height, kind], j) => (
                  <span key={j} className={`${s.block} ${s[kind]}`} style={{ top: `${top}%`, height: `${height}%` }} />
                ))}
              </div>
            ))}
          </div>
        </div>
      </aside>

      <main className={s.formSide}>
        <form className={s.form} onSubmit={onSubmit} noValidate>
          <div>
            <h1>ברוכים הבאים</h1>
            <p className="muted">התחברו לחשבון שלכם.</p>
          </div>

          <div className="field">
            <label htmlFor="email">אימייל</label>
            <input id="email" className="input" type="email" autoComplete="username" dir="ltr" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@hospital.org" />
          </div>

          <div className="field">
            <label htmlFor="password">סיסמה</label>
            <div className={s.pw}>
              <input id="password" className="input" type={show ? 'text' : 'password'} autoComplete="current-password" dir="ltr" required value={password} onChange={(e) => setPassword(e.target.value)} />
              <button type="button" className={s.eye} onClick={() => setShow((v) => !v)} aria-label={show ? 'הסתר סיסמה' : 'הצג סיסמה'}>
                {show ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>

          {error && <div className={s.error} role="alert">{error}</div>}

          <Button type="submit" loading={busy} disabled={!email || !password} icon={<LogIn size={18} />}>התחברות</Button>
          <p className={s.alt}>עדיין אין לך חשבון? <Link to="/register">הרשמה</Link></p>
        </form>
      </main>
    </div>
  );
}
