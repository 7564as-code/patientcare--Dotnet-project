import type { ReactNode } from 'react';
import { AlertTriangle, Loader2 } from 'lucide-react';
import { Button } from './Button';
import s from './States.module.css';

export function Spinner({ label = 'טוען…' }: { label?: string }) {
  return (
    <div className={s.center} role="status">
      <Loader2 className={s.spin} size={22} aria-hidden />
      <span>{label}</span>
    </div>
  );
}

export function EmptyState({ icon, title, hint, action }: { icon?: ReactNode; title: string; hint?: string; action?: ReactNode }) {
  return (
    <div className={s.empty}>
      {icon && <div className={s.iconWrap}>{icon}</div>}
      <h3>{title}</h3>
      {hint && <p>{hint}</p>}
      {action}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className={s.empty} role="alert">
      <div className={`${s.iconWrap} ${s.err}`}><AlertTriangle size={22} aria-hidden /></div>
      <h3>לא הצלחנו לטעון את הנתונים</h3>
      <p>{message}</p>
      {onRetry && <Button variant="secondary" size="sm" onClick={onRetry}>נסה שוב</Button>}
    </div>
  );
}
