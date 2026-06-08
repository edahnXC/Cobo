import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SubscriptionService } from '../core/subscription.service';
import { AuthStateService } from '../core/auth-state.service';
import { Plan, Subscription } from '../models';

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="pricing-container">
      <header class="pricing-header">
        <a routerLink="/workspaces" class="back-link">← Back to Workspaces</a>
        <h1 class="gradient-text">Premium Subscription Tiers</h1>
        <p class="subtitle">Choose the perfect plan for your team and student group</p>
      </header>

      <!-- Current Subscription Status -->
      <section class="current-sub-card glass-panel" *ngIf="authState.isAuthenticated() && mySubscription()">
        <div class="sub-badge success" *ngIf="mySubscription()?.isActive">Active Subscription</div>
        <div class="sub-badge warning" *ngIf="!mySubscription()?.isActive">No Active Subscription</div>
        
        <div class="sub-details">
          <h3>Your Current Plan: <span class="gradient-text">{{ mySubscription()?.planName }}</span></h3>
          <p class="sub-dates" *ngIf="mySubscription()?.isActive">
            Period: {{ mySubscription()?.startDate | date:'shortDate' }} to {{ mySubscription()?.endDate | date:'shortDate' }}
          </p>
          <p class="sub-dates" *ngIf="!mySubscription()?.isActive">
            Upgrade below to unlock premium voice and game rooms.
          </p>
        </div>

        <button 
          *ngIf="mySubscription()?.isActive && mySubscription()?.planName !== 'Free'" 
          (click)="cancelSubscription()" 
          [disabled]="cancelling()"
          class="btn-secondary cancel-btn"
        >
          <span *ngIf="cancelling()">Cancelling...</span>
          <span *ngIf="!cancelling()">Cancel Subscription</span>
        </button>
      </section>

      <!-- Plans Grid -->
      <main class="plans-grid">
        <div 
          *ngFor="let plan of plans()" 
          class="plan-card glass-panel"
          [class.featured]="plan.planName === 'Pro'"
        >
          <div class="featured-tag" *ngIf="plan.planName === 'Pro'">MOST POPULAR</div>
          <h2 class="plan-name">{{ plan.planName }}</h2>
          <div class="price-container">
            <span class="currency">₹</span>
            <span class="price">{{ plan.price }}</span>
            <span class="period">/month</span>
          </div>
          
          <p class="plan-members">
            Up to <strong>{{ plan.maxMembers || 'Unlimited' }}</strong> members
          </p>

          <ul class="features-list">
            <li *ngFor="let feat of plan.features">✓ {{ feat }}</li>
          </ul>

          <button 
            *ngIf="authState.isAuthenticated() && authState.user()?.currentPlan !== plan.planName"
            (click)="checkout(plan)"
            [disabled]="loadingPlanId() === plan.planId"
            class="gradient-btn upgrade-action-btn"
          >
            <span *ngIf="loadingPlanId() === plan.planId">Redirecting...</span>
            <span *ngIf="loadingPlanId() !== plan.planId">
              {{ plan.price === 0 ? 'Downgrade' : 'Upgrade to ' + plan.planName }}
            </span>
          </button>

          <div class="current-plan-indicator" *ngIf="authState.isAuthenticated() && authState.user()?.currentPlan === plan.planName">
            🎯 Your Active Plan
          </div>
          
          <a routerLink="/login" class="gradient-btn upgrade-action-btn" *ngIf="!authState.isAuthenticated()">
            Sign In to Subscribe
          </a>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .pricing-container {
      min-height: 100vh;
      background: radial-gradient(circle at 10% 10%, rgba(98, 0, 234, 0.08) 0%, transparent 40%),
                  radial-gradient(circle at 90% 90%, rgba(0, 242, 254, 0.08) 0%, transparent 40%),
                  var(--bg-primary);
      padding: 40px 20px;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 30px;
    }
    .pricing-header {
      text-align: center;
      max-width: 600px;
    }
    .back-link {
      display: inline-block;
      margin-bottom: 20px;
      font-size: 0.9rem;
    }
    .subtitle {
      color: var(--text-muted);
      margin-top: 8px;
    }
    .current-sub-card {
      width: 100%;
      max-width: 860px;
      padding: 24px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 20px;
      border: 1px solid var(--primary-glow);
    }
    @media (max-width: 768px) {
      .current-sub-card {
        flex-direction: column;
        text-align: center;
      }
    }
    .sub-badge {
      padding: 6px 12px;
      border-radius: 20px;
      font-size: 0.8rem;
      font-weight: 700;
    }
    .sub-badge.success { background: rgba(0, 230, 118, 0.15); color: var(--success); }
    .sub-badge.warning { background: rgba(255, 23, 68, 0.15); color: var(--danger); }
    
    .sub-details h3 {
      font-size: 1.2rem;
      margin-bottom: 4px;
    }
    .sub-dates {
      font-size: 0.88rem;
      color: var(--text-muted);
    }
    .cancel-btn:hover {
      border-color: var(--danger) !important;
      color: var(--danger);
    }
    .plans-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(270px, 1fr));
      gap: 30px;
      width: 100%;
      max-width: 860px;
    }
    .plan-card {
      padding: 40px 30px;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      position: relative;
    }
    .plan-card.featured {
      border: 2px solid var(--primary);
      box-shadow: 0 10px 30px var(--primary-glow);
    }
    .featured-tag {
      position: absolute;
      top: -12px;
      background: var(--primary);
      color: white;
      font-size: 0.75rem;
      font-weight: bold;
      padding: 4px 12px;
      border-radius: 20px;
    }
    .plan-name {
      font-size: 1.5rem;
      margin-bottom: 15px;
    }
    .price-container {
      margin-bottom: 10px;
    }
    .currency {
      font-size: 1.8rem;
      font-weight: 700;
      vertical-align: top;
      margin-right: 2px;
    }
    .price {
      font-size: 3.2rem;
      font-weight: 800;
      font-family: 'Outfit', sans-serif;
    }
    .period {
      color: var(--text-muted);
      font-size: 0.9rem;
    }
    .plan-members {
      font-size: 0.88rem;
      margin-bottom: 25px;
      color: var(--text-secondary);
    }
    .features-list {
      list-style: none;
      text-align: left;
      display: flex;
      flex-direction: column;
      gap: 12px;
      margin-bottom: 35px;
      flex-grow: 1;
      width: 100%;
      padding-left: 20px;
    }
    .features-list li {
      font-size: 0.92rem;
      color: var(--text-secondary);
    }
    .upgrade-action-btn {
      width: 100%;
      padding: 12px;
      font-size: 0.95rem;
    }
    .current-plan-indicator {
      font-weight: bold;
      color: var(--accent);
      padding: 10px;
    }
  `]
})
export class PlansComponent implements OnInit {
  plans = signal<Plan[]>([]);
  mySubscription = signal<Subscription | null>(null);
  loadingPlanId = signal<number | null>(null);
  cancelling = signal(false);

  constructor(
    private subService: SubscriptionService,
    readonly authState: AuthStateService
  ) {}

  ngOnInit(): void {
    this.subService.getPlans().subscribe(data => {
      // Sort plans by price
      this.plans.set(data.sort((a,b) => a.price - b.price));
    });

    if (this.authState.isAuthenticated()) {
      this.subService.getMySubscription().subscribe(sub => {
        this.mySubscription.set(sub);
      });
    }
  }

  checkout(plan: Plan): void {
    if (plan.price === 0) {
      // Free plan downgrade/cancel
      this.cancelSubscription();
      return;
    }

    if (!plan.stripePriceId) {
      alert('Stripe configuration missing for this plan.');
      return;
    }

    this.loadingPlanId.set(plan.planId);
    const successUrl = `${window.location.origin}/workspaces`;
    const cancelUrl = window.location.href;

    this.subService.createCheckout(plan.stripePriceId, successUrl, cancelUrl).subscribe({
      next: (res) => {
        window.location.href = res.checkoutUrl;
      },
      error: (err) => {
        this.loadingPlanId.set(null);
        alert(err.error?.error || 'Failed to initiate checkout.');
      }
    });
  }

  cancelSubscription(): void {
    if (!confirm('Are you sure you want to cancel your premium subscription? You will lose access to premium channels at the end of the billing period.')) return;
    this.cancelling.set(true);

    this.subService.cancelSubscription().subscribe({
      next: () => {
        this.cancelling.set(false);
        alert('Subscription successfully cancelled.');
        window.location.reload();
      },
      error: (err) => {
        this.cancelling.set(false);
        alert(err.error?.error || 'Failed to cancel subscription.');
      }
    });
  }
}
