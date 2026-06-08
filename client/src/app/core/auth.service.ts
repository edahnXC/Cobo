import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthStateService } from './auth-state.service';
import { User, AuthResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient, private authState: AuthStateService) {}

  register(request: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/register`, request);
  }

  verifyEmail(token: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/verify-email`, { params: { token } });
  }

  resendVerification(email: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/resend-verification`, { email });
  }

  login(request: any): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(response => {
        this.authState.setSession(response.accessToken, response.refreshToken, response.user);
      })
    );
  }

  logout(): Observable<any> {
    const refreshToken = this.authState.getRefreshToken() || '';
    return this.http.post<any>(`${this.baseUrl}/logout`, { refreshToken }).pipe(
      tap({
        finalize: () => this.authState.clearSession()
      })
    );
  }

  me(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`).pipe(
      tap(user => {
        this.authState.updateUser(user);
      })
    );
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(request: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/reset-password`, request);
  }
}
