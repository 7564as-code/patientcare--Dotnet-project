import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { AlertCircle, CheckCircle2 } from 'lucide-react';
import s from './Toast.module.css';

type Kind = 'success' | 'error';
interface ToastItem { id: number; kind: Kind; text: string }
interface ToastApi { success: (t: string) => void; error: (t: string) => void }

const ToastContext = createContext<ToastApi | null>(null);
let seq = 0;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);

  const push = useCallback((kind: Kind, text: string) => {
    const id = ++seq;
    setItems((x) => [...x, { id, kind, text }]);
    setTimeout(() => setItems((x) => x.filter((i) => i.id !== id)), 4500);
  }, []);

  const api = useMemo<ToastApi>(() => ({ success: (t) => push('success', t), error: (t) => push('error', t) }), [push]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className={s.stack} aria-live="polite">
        {items.map((i) => (
          <div key={i.id} className={`${s.toast} ${s[i.kind]}`}>
            {i.kind === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
            <span>{i.text}</span>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside <ToastProvider>');
  return ctx;
}
