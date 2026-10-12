export interface TokenClaims {
  caregiverId: number | null;
  email: string | null;
  name: string | null;
  gender: string | null;
  exp: number | null;
}

const NAME_URI = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const ID_URI = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

function decodePayload(token: string): Record<string, unknown> | null {
  try {
    const part = token.split('.')[1];
    const b64 = part.replace(/-/g, '+').replace(/_/g, '/');
    const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    return null;
  }
}

/** קורא את ה-claims שה-AuthService מכניס לטוקן: sub, email, name, gender. */
export function parseToken(token: string): TokenClaims | null {
  const p = decodePayload(token);
  if (!p) return null;
  const rawId = p['sub'] ?? p[ID_URI];
  const id = rawId !== undefined ? Number(rawId) : NaN;
  return {
    caregiverId: Number.isFinite(id) ? id : null,
    email: (p['email'] as string) ?? null,
    name: ((p['unique_name'] ?? p['name'] ?? p[NAME_URI]) as string) ?? null,
    gender: (p['gender'] as string) ?? null,
    exp: typeof p['exp'] === 'number' ? (p['exp'] as number) : null,
  };
}

export function isExpired(token: string): boolean {
  const c = parseToken(token);
  if (!c?.exp) return false;
  return c.exp * 1000 <= Date.now();
}
