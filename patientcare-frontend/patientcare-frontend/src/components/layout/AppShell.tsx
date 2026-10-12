import { NavLink, Outlet } from 'react-router-dom';
import { ArrowLeftRight, CalendarDays, LayoutDashboard, LogOut, Users } from 'lucide-react';
import { useAuth } from '@/auth/AuthContext';
import { swapRequestsApi } from '@/api';
import { useAsync } from '@/hooks/useAsync';
import { initials } from '@/utils/labels';
import s from './AppShell.module.css';

const NAV = [
  { to: '/', label: 'לוח בקרה', icon: LayoutDashboard, end: true },
  { to: '/shifts', label: 'משמרות', icon: CalendarDays },
  { to: '/patients', label: 'חולים', icon: Users },
  { to: '/swaps', label: 'בקשות החלפה', icon: ArrowLeftRight, badge: true },
];

export function AppShell() {
  const { user, logout } = useAuth();
  const pending = useAsync(() => swapRequestsApi.incoming().catch(() => []), []);
  const pendingCount = pending.data?.length ?? 0;

  return (
    <div className={s.shell}>
      <aside className={s.sidebar}>
        <div className={s.brand}>
          <span className={s.logo} aria-hidden>
            <svg viewBox="0 0 32 32" width="22" height="22"><path d="M16 7v18M7 16h18" stroke="currentColor" strokeWidth="3.6" strokeLinecap="round" /></svg>
          </span>
          <span className={s.brandName}>PatientCare</span>
        </div>
        <nav className={s.nav} aria-label="ניווט ראשי">
          {NAV.map(({ to, label, icon: Icon, end, badge }) => (
            <NavLink key={to} to={to} end={end} className={({ isActive }) => `${s.link} ${isActive ? s.active : ''}`}>
              <span className={s.iconBox}>
                <Icon size={20} aria-hidden />
                {badge && pendingCount > 0 && <span className={s.dot} aria-label={`${pendingCount} בקשות ממתינות`}>{pendingCount}</span>}
              </span>
              <span className={s.label}>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className={s.user}>
          <span className={s.avatar} aria-hidden>{initials(user?.fullName ?? '?')}</span>
          <div className={s.userText}>
            <strong>{user?.fullName}</strong>
            <span>{user?.email}</span>
          </div>
          <button className={s.logout} onClick={logout} aria-label="התנתקות" title="התנתקות"><LogOut size={18} /></button>
        </div>
      </aside>
      <header className={s.topbar}>
        <span className={s.logo} aria-hidden>
          <svg viewBox="0 0 32 32" width="18" height="18"><path d="M16 7v18M7 16h18" stroke="currentColor" strokeWidth="3.6" strokeLinecap="round" /></svg>
        </span>
        <strong>PatientCare</strong>
        <span className={s.topUser}>{user?.fullName}</span>
        <button className={s.topLogout} onClick={logout} aria-label="התנתקות"><LogOut size={18} /></button>
      </header>
      <main className={s.main}>
        <Outlet />
      </main>
    </div>
  );
}
