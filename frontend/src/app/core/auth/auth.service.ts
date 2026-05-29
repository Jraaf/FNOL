import { Injectable, signal } from '@angular/core';

export type AppRole = 'Handler' | 'Supervisor' | 'Manager';

export interface MockUser {
  userId: string;
  userName: string;
  role: AppRole;
  mockJwt: string;
}

const USERS: MockUser[] = [
  { userId: 'handler-1', userName: 'Hayley Handler', role: 'Handler', mockJwt: buildJwt('handler-1', 'Hayley Handler', 'Handler') },
  { userId: 'supervisor-1', userName: 'Sam Supervisor', role: 'Supervisor', mockJwt: buildJwt('supervisor-1', 'Sam Supervisor', 'Supervisor') },
  { userId: 'manager-1', userName: 'Morgan Manager', role: 'Manager', mockJwt: buildJwt('manager-1', 'Morgan Manager', 'Manager') }
];

function base64UrlEncode(value: string): string {
  return btoa(value).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
}

function buildJwt(sub: string, name: string, role: AppRole): string {
  const header = base64UrlEncode(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const payload = base64UrlEncode(JSON.stringify({ sub, name, role, iat: Math.floor(Date.now() / 1000) }));
  return `${header}.${payload}.`;
}

const STORAGE_KEY = 'claims.mockUser';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly users = USERS;
  readonly current = signal<MockUser>(this.restore());

  setRole(role: AppRole): void {
    const user = USERS.find(u => u.role === role) ?? USERS[0];
    this.current.set(user);
    localStorage.setItem(STORAGE_KEY, user.role);
  }

  hasRole(role: AppRole): boolean {
    return this.current().role === role;
  }

  hasAnyRole(roles: AppRole[]): boolean {
    return roles.includes(this.current().role);
  }

  private restore(): MockUser {
    const stored = localStorage.getItem(STORAGE_KEY) as AppRole | null;
    return USERS.find(u => u.role === stored) ?? USERS[0];
  }
}
