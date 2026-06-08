import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="glass-panel auth-card">
        <h2 class="gradient-text auth-title">Reset Password</h2>
        <p class="auth-subtitle">Enter your email and we'll send you a password reset link</p>

        <form (ngSubmit)="onSubmit()" #forgotForm="ngForm" class="auth-form" *ngIf="!submitted()">
          <div *ngIf="errorMessage()" class="error-banner">
            {{ errorMessage() }}
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

          <button
            type="submit"
            [disabled]="forgotForm.invalid || isLoading()"
            class="gradient-btn submit-btn"
          >
            <span *ngIf="isLoading()">Sending email...</span>
            <span *ngIf="!isLoading()">Send Reset Link</span>
          </button>
        </form>

        <div *ngIf="submitted()" class="success-view">
          <div class="success-icon">✉️</div>
          <h3>Reset Email Sent</h3>
          <p>We've sent a password reset link to <strong>{{ email }}</strong>. Please check your inbox and follow the instructions to reset your password.</p>
          <a routerLink="/login" class="back-login-btn gradient-btn">Back to Sign In</a>
        </div>

        <div class="auth-footer" *ngIf="!submitted()">
          Remembered your password? <a routerLink="/login">Sign in</a>
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
    .success-view {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 15px;
      animation: fadeIn 0.3s ease-out;
    }
    .success-icon {
      font-size: 3rem;
      margin-bottom: 10px;
    }
    .back-login-btn {
      margin-top: 15px;
      display: inline-block;
      width: 100%;
      text-align: center;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(15px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class ForgotPasswordComponent {
  email = '';
  isLoading = signal(false);
  submitted = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private authService: AuthService) {}

  onSubmit(): void {
    if (this.isLoading()) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.forgotPassword(this.email).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to send reset link. Verify your email address.');
      }
    });
  }
}
