import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="glass-panel auth-card">
        <h2 class="gradient-text auth-title">Email Verification</h2>

        <!-- Loading State -->
        <div *ngIf="status() === 'verifying'" class="status-view">
          <div class="loading-spinner"></div>
          <p>Verifying your email address, please wait...</p>
        </div>

        <!-- Success State -->
        <div *ngIf="status() === 'success'" class="status-view success">
          <div class="status-icon">✅</div>
          <h3>Email Verified!</h3>
          <p>Thank you! Your email has been successfully verified. You can now access your workspaces.</p>
          <a routerLink="/login" class="gradient-btn action-btn">Sign In Now</a>
        </div>

        <!-- Error State -->
        <div *ngIf="status() === 'error'" class="status-view error">
          <div class="status-icon">❌</div>
          <h3>Verification Failed</h3>
          <p class="error-msg">{{ errorMessage() }}</p>
          <p>The verification link may have expired or is invalid. Please request a new verification link from the login page.</p>
          <a routerLink="/login" class="btn-secondary action-btn">Back to Sign In</a>
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
      max-width: 460px;
      padding: 40px;
      text-align: center;
      animation: fadeIn 0.5s ease-out;
    }
    .auth-title {
      font-size: 1.8rem;
      margin-bottom: 25px;
    }
    .status-view {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 15px;
    }
    .status-icon {
      font-size: 3.5rem;
      margin-bottom: 10px;
    }
    .loading-spinner {
      border: 4px solid rgba(255, 255, 255, 0.1);
      width: 50px;
      height: 50px;
      border-radius: 50%;
      border-left-color: var(--primary);
      animation: spin 1s linear infinite;
      margin-bottom: 10px;
    }
    .action-btn {
      width: 100%;
      padding: 12px;
      margin-top: 15px;
      display: inline-block;
    }
    .error-msg {
      color: #ff5252;
      font-weight: 500;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(15px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class VerifyEmailComponent implements OnInit {
  status = signal<'verifying' | 'success' | 'error'>('verifying');
  errorMessage = signal<string>('Unknown error');

  constructor(private route: ActivatedRoute, private authService: AuthService) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.status.set('error');
      this.errorMessage.set('No verification token provided in the link.');
      return;
    }

    this.authService.verifyEmail(token).subscribe({
      next: () => {
        this.status.set('success');
      },
      error: (err) => {
        this.status.set('error');
        this.errorMessage.set(err.error?.error || 'Verification link expired or invalid.');
      }
    });
  }
}
