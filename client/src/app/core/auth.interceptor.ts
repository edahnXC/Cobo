import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { AuthStateService } from './auth-state.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthResponse } from '../models';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshToken$ = new BehaviorSubject<string | null>(null);

  constructor(private authState: AuthStateService, private http: HttpClient) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip auth header for auth endpoints
    if (req.url.includes('/auth/login') || req.url.includes('/auth/register') ||
        req.url.includes('/auth/refresh') || req.url.includes('/auth/verify-email') ||
        req.url.includes('/auth/forgot-password') || req.url.includes('/auth/reset-password') ||
        req.url.includes('/plans')) {
      return next.handle(req);
    }

    const token = this.authState.accessToken();
    const authReq = token ? this.addToken(req, token) : req;

    return next.handle(authReq).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401 && !req.url.includes('/auth/')) {
          return this.handle401(req, next);
        }
        return throwError(() => err);
      })
    );
  }

  private addToken(req: HttpRequest<any>, token: string): HttpRequest<any> {
    return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  private handle401(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshToken$.next(null);

      const refreshToken = this.authState.getRefreshToken();
      if (!refreshToken) {
        this.authState.clearSession();
        return throwError(() => new Error('No refresh token'));
      }

      return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken }).pipe(
        switchMap(response => {
          this.isRefreshing = false;
          this.authState.setSession(response.accessToken, response.refreshToken, response.user);
          this.refreshToken$.next(response.accessToken);
          return next.handle(this.addToken(req, response.accessToken));
        }),
        catchError(err => {
          this.isRefreshing = false;
          this.authState.clearSession();
          return throwError(() => err);
        })
      );
    }

    return this.refreshToken$.pipe(
      filter(token => token !== null),
      take(1),
      switchMap(token => next.handle(this.addToken(req, token!)))
    );
  }
}
