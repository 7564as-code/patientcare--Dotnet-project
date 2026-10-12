import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi, setUnauthorizedHandler } from '@/api';
import type { RegisterRequest } from '@/types/dto';
import { isExpired, parseToken } from './jwt';
import { tokenStorage } from './tokenStorage';

export interface AuthUser {
  caregiverId: number | null;
  fullName: string;
  email: string | null;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (body: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readUser(): AuthUser | null {
  const stored = tokenStorage.get();
  if (!stored || isExpired(stored.token)) {
    tokenStorage.clear();
    return null;
  }
  const claims = parseToken(stored.token);
  return { caregiverId: claims?.caregiverId ?? null, fullName: stored.fullName, email: claims?.email ?? null };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readUser);

  const logout = useCallback(() => {
    tokenStorage.clear();
    setUser(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login({ email, password });
    tokenStorage.set({ token: res.token, fullName: res.fullName, expiresAt: res.expiresAt });
    setUser(readUser());
  }, []);

  const register = useCallback(async (body: RegisterRequest) => {
    const res = await authApi.register(body);
    tokenStorage.set({ token: res.token, fullName: res.fullName, expiresAt: res.expiresAt });
    setUser(readUser());
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  const value = useMemo(() => ({ user, isAuthenticated: !!user, login, register, logout }), [user, login, register, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
