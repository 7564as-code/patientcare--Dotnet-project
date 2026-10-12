const KEY = 'patientcare.auth';

export interface StoredAuth {
  token: string;
  fullName: string;
  expiresAt: string;
}

export const tokenStorage = {
  get(): StoredAuth | null {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as StoredAuth) : null;
    } catch {
      return null;
    }
  },
  set(value: StoredAuth) {
    try {
      localStorage.setItem(KEY, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  },
  clear() {
    try {
      localStorage.removeItem(KEY);
    } catch {
      /* ignore */
    }
  },
};
