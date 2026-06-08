import { Component, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="glass-panel auth-card">
        <h2 class="gradient-text auth-title">Welcome Back</h2>
        <p class="auth-subtitle">Sign in to your collaborative workspace</p>

        <form (ngSubmit)="onSubmit()" #loginForm="ngForm" class="auth-form">
          <div *ngIf="errorMessage()" class="error-banner">
            {{ errorMessage() }}
          </div>
          
          <div *ngIf="successMessage()" class="success-banner">
            {{ successMessage() }}
          </div>

          <div class="form-group">
            <label class="form-label" for="email">Email Address</label>
            <input
              type="email"
              id="email"
              name="email"
              [(ngModel)]="email"
              required
              email
              #emailInput="ngModel"
              class="form-input"
              placeholder="name@company.com"
            />
            <div *ngIf="emailInput.invalid && emailInput.touched" class="error-text">
              Please enter a valid email address.
            </div>
          </div>

          <div class="form-group">
            <div class="label-row">
              <label class="form-label" for="password">Password</label>
              <a routerLink="/forgot-password" class="forgot-link">Forgot password?</a>
            </div>
            <input
              type="password"
              id="password"
              name="password"
              [(ngModel)]="password"
              required
              minlength="6"
              #passwordInput="ngModel"
              class="form-input"
              placeholder="••••••••"
            />
            <div *ngIf="passwordInput.invalid && passwordInput.touched" class="error-text">
              Password must be at least 6 characters.
            </div>
          </div>

          <button
            type="submit"
            [disabled]="loginForm.invalid || isLoading()"
            class="gradient-btn submit-btn"
          >
            <span *ngIf="isLoading()">Signing in...</span>
            <span *ngIf="!isLoading()">Sign In</span>
          </button>
        </form>

        <div class="auth-footer">
          Don't have an account? <a routerLink="/register">Register here</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: radial-gradient(circle at 10% 20%, rgba(98, 0, 234, 0.15) 0%, transparent 40%),
                  radial-gradient(circle at 90% 80%, rgba(0, 242, 254, 0.12) 0%, transparent 40%),
                  var(--bg-primary);
      padding: 20px;
    }
    .auth-card {
      width: 100%;
      max-width: 440px;
      padding: 40px;
      animation: fadeIn 0.5s ease-out;
    }
    .auth-title {
      font-size: 2rem;
      text-align: center;
      margin-bottom: 8px;
    }
    .auth-subtitle {
      color: var(--text-muted);
      text-align: center;
      margin-bottom: 30px;
      font-size: 0.95rem;
    }
    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 15px;
    }
    .label-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .forgot-link {
      font-size: 0.8rem;
    }
    .submit-btn {
      margin-top: 10px;
      padding: 12px;
      font-size: 1rem;
    }
    .auth-footer {
      text-align: center;
      margin-top: 25px;
      font-size: 0.88rem;
      color: var(--text-muted);
    }
    .error-banner {
      background: rgba(255, 23, 68, 0.12);
      border: 1px solid var(--danger);
      color: #ff5252;
      padding: 10px 14px;
      border-radius: var(--border-radius-sm);
      font-size: 0.88rem;
    }
    .success-banner {
      background: rgba(0, 230, 118, 0.12);
      border: 1px solid var(--success);
      color: #69f0ae;
      padding: 10px 14px;
      border-radius: var(--border-radius-sm);
      font-size: 0.88rem;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(15px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class LoginComponent {
  email = '';
  password = '';
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(): void {
    if (this.isLoading()) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.router.navigate(['/workspaces']);
      },
      error: (err) => {
        this.isLoading.set(false);
        const code = err.error?.code;
        if (code === 'EMAIL_NOT_VERIFIED') {
          this.errorMessage.set('Your email address has not been verified. Please check your inbox or request a new verification link.');
        } else {
          this.errorMessage.set(err.error?.error || 'Failed to sign in. Please verify your credentials.');
        }
      }
    });
  }
}
