import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="glass-panel auth-card">
        <h2 class="gradient-text auth-title">Set New Password</h2>
        <p class="auth-subtitle" *ngIf="!submitted()">Enter your new password below</p>

        <form (ngSubmit)="onSubmit()" #resetForm="ngForm" class="auth-form" *ngIf="!submitted() && token()">
          <div *ngIf="errorMessage()" class="error-banner">
            {{ errorMessage() }}
          </div>

          <div class="form-group">
            <label class="form-label" for="newPassword">New Password</label>
            <input
              type="password"
              id="newPassword"
              name="newPassword"
              [(ngModel)]="newPassword"
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

          <div class="form-group">
            <label class="form-label" for="confirmPassword">Confirm Password</label>
            <input
              type="password"
              id="confirmPassword"
              name="confirmPassword"
              [(ngModel)]="confirmPassword"
              required
              #confirmInput="ngModel"
              class="form-input"
              placeholder="••••••••"
            />
            <div *ngIf="newPassword !== confirmPassword && confirmInput.touched" class="error-text">
              Passwords do not match.
            </div>
          </div>

          <button
            type="submit"
            [disabled]="resetForm.invalid || newPassword !== confirmPassword || isLoading()"
            class="gradient-btn submit-btn"
          >
            <span *ngIf="isLoading()">Resetting password...</span>
            <span *ngIf="!isLoading()">Reset Password</span>
          </button>
        </form>

        <div *ngIf="!token()" class="error-banner" style="margin-bottom: 20px;">
          Invalid reset password link. No token was found.
        </div>

        <div *ngIf="submitted()" class="success-view">
          <div class="success-icon">🎉</div>
          <h3>Password Reset Successful</h3>
          <p>Your password has been reset successfully. You can now use your new password to sign in.</p>
          <a routerLink="/login" class="back-login-btn gradient-btn">Sign In Now</a>
        </div>

        <div class="auth-footer" *ngIf="!submitted()">
          Back to <a routerLink="/login">Sign in</a>
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
export class ResetPasswordComponent implements OnInit {
  newPassword = '';
  confirmPassword = '';
  token = signal<string | null>(null);
  isLoading = signal(false);
  submitted = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private route: ActivatedRoute, private authService: AuthService) {}

  ngOnInit(): void {
    this.token.set(this.route.snapshot.queryParamMap.get('token'));
  }

  onSubmit(): void {
    const token = this.token();
    if (!token || this.isLoading()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const payload = {
      token: token,
      newPassword: this.newPassword
    };

    this.authService.resetPassword(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.error || 'Failed to reset password. The link might have expired or been used already.');
      }
    });
  }
}
