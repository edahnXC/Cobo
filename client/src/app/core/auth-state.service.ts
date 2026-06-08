import { Injectable, signal, computed } from '@angular/core';
import { User } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly _user = signal<User | null>(null);
  private readonly _accessToken = signal<string | null>(null);
  private readonly _refreshToken = signal<string | null>(null);

  readonly user = this._user.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isAuthenticated = computed(() => !!this._accessToken());
  readonly isPlatformAdmin = computed(() => this._user()?.roles.includes('PlatformAdmin') ?? false);
  readonly isPremium = computed(() => {
    const plan = this._user()?.currentPlan ?? 'Free';
    return plan === 'Pro' || plan === 'Enterprise';
  });

  constructor() {
    this.restoreSession();
  }

  setSession(accessToken: string, refreshToken: string, user: User): void {
    this._accessToken.set(accessToken);
    this._refreshToken.set(refreshToken);
    this._user.set(user);
    localStorage.setItem('refresh_token', refreshToken);
    localStorage.setItem('user', JSON.stringify(user));
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._refreshToken.set(null);
    this._user.set(null);
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
  }

  getRefreshToken(): string | null {
    return this._refreshToken();
  }

  updateToken(accessToken: string): void {
    this._accessToken.set(accessToken);
  }

  updateUser(user: User): void {
    this._user.set(user);
    localStorage.setItem('user', JSON.stringify(user));
  }

  private restoreSession(): void {
    const storedUser = localStorage.getItem('user');
    const storedRefresh = localStorage.getItem('refresh_token');
    if (storedUser && storedRefresh) {
      this._user.set(JSON.parse(storedUser));
      this._refreshToken.set(storedRefresh);
      // access token will be refreshed on first API call
    }
  }
}
