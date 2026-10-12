import type { ReactNode } from 'react';
import type { Tone } from '@/utils/labels';
import s from './Badge.module.css';

export function Badge({ tone = 'neutral', icon, children }: { tone?: Tone; icon?: ReactNode; children: ReactNode }) {
  return (
    <span className={`${s.badge} ${s[tone]}`}>
      {icon}
      {children}
    </span>
  );
}
