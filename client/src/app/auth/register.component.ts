import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="glass-panel auth-card">
        <h2 class="gradient-text auth-title">Create Account</h2>
        <p class="auth-subtitle">Join the private collaborative platform</p>

        <form (ngSubmit)="onSubmit()" #registerForm="ngForm" class="auth-form" *ngIf="!registered()">
          <div *ngIf="errorMessage()" class="error-banner">
            {{ errorMessage() }}
          </div>

          <div class="form-group">
            <label class="form-label" for="fullName">Full Name</label>
            <input
              type="text"
              id="fullName"
              name="fullName"
              [(ngModel)]="fullName"
              required
              minlength="2"
              #nameInput="ngModel"
              class="form-input"
              placeholder="Alex Johnson"
            />
            <div *ngIf="nameInput.invalid && nameInput.touched" class="error-text">
              Name is required (at least 2 characters).
            </div>
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
            <label class="form-label" for="password">Password</label>
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
            [disabled]="registerForm.invalid || isLoading()"
            class="gradient-btn submit-btn"
          >
            <span *ngIf="isLoading()">Creating account...</span>
            <span *ngIf="!isLoading()">Register</span>
          </button>
        </form>

        <!-- Success view -->
        <div *ngIf="registered()" class="success-view">
          <div class="success-icon">✉️</div>
          <h3>Verify your email</h3>
          <p>We've sent a verification link to <strong>{{ email }}</strong>. Please check your inbox (and spam folder) to activate your account.</p>
          <button (click)="resendVerification()" [disabled]="isResending()" class="btn-secondary resend-btn">
            <span *ngIf="isResending()">Resending link...</span>
            <span *ngIf="!isResending()">Resend Email</span>
          </button>
          <div *ngIf="resendMessage()" class="resend-info-msg">
            {{ resendMessage() }}
          </div>
          <a routerLink="/login" class="back-login-btn gradient-btn">Back to Sign In</a>
        </div>

        <div class="auth-footer" *ngIf="!registered()">
          Already have an account? <a routerLink="/login">Sign in here</a>
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
    .resend-btn {
      width: 100%;
      padding: 10px;
      margin-top: 10px;
    }
    .resend-info-msg {
      font-size: 0.85rem;
      color: var(--success);
      margin-top: 5px;
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
export class RegisterComponent {
  fullName = '';
  email = '';
  password = '';
  isLoading = signal(false);
  registered = signal(false);
  errorMessage = signal<string | null>(null);
  isResending = signal(false);
  resendMessage = signal<string | null>(null);

  constructor(private authService: AuthService) {}

  onSubmit(): void {
    if (this.isLoading()) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const payload = {
      fullName: this.fullName,
      email: this.email,
      password: this.password
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.registered.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to register. Email might already be taken.');
      }
    });
  }

  resendVerification(): void {
    if (this.isResending()) return;
    this.isResending.set(true);
    this.resendMessage.set(null);

    this.authService.resendVerification(this.email).subscribe({
      next: (res) => {
        this.isResending.set(false);
        this.resendMessage.set(res.message || 'Verification email resent successfully.');
      },
      error: (err) => {
        this.isResending.set(false);
        this.resendMessage.set(err.error?.error || 'Failed to resend email. Please try again later.');
      }
    });
  }
}
