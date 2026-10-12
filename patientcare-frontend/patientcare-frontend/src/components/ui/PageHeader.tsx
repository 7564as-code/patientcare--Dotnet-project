import type { ReactNode } from 'react';
import s from './PageHeader.module.css';

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className={s.head}>
      <div>
        <h1>{title}</h1>
        {subtitle && <p className={s.sub}>{subtitle}</p>}
      </div>
      {actions && <div className={s.actions}>{actions}</div>}
    </div>
  );
}
