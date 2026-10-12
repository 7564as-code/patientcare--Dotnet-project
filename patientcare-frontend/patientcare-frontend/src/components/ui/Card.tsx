import type { HTMLAttributes } from 'react';
import s from './Card.module.css';

export function Card({ className, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return <div {...rest} className={[s.card, className].filter(Boolean).join(' ')} />;
}
