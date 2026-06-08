import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ComplaintService } from '../core/complaint.service';
import { Complaint } from '../models';

@Component({
  selector: 'app-complaints',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="complaints-container">
      <header class="complaints-header">
        <a routerLink="/workspaces" class="back-link">← Back to Workspaces</a>
        <h1 class="gradient-text">Support & Incident Reports</h1>
        <p class="subtitle">Submit incident reports or general platform technical feedback</p>
      </header>

      <div class="complaints-layout">
        <!-- New Complaint Form -->
        <section class="form-section glass-panel">
          <h2>File a New Report</h2>
          <form (ngSubmit)="onSubmit()" #complaintForm="ngForm">
            <div class="form-group">
              <label class="form-label" for="category">Category</label>
              <select 
                id="category" 
                name="category" 
                [(ngModel)]="category" 
                required 
                class="form-input"
              >
                <option value="Harassment">Harassment / Bullying</option>
                <option value="Spam">Spam / Advertising</option>
                <option value="InappropriateContent">Inappropriate Content</option>
                <option value="TechnicalIssue">Technical Bug / Issue</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label" for="description">Details / Description</label>
              <textarea 
                id="description" 
                name="description" 
                [(ngModel)]="description" 
                required 
                minlength="10"
                #descInput="ngModel"
                class="form-input" 
                rows="5"
                placeholder="Describe the incident or technical issue in detail. If reporting a user, please specify their email or display name..."
              ></textarea>
              <div *ngIf="descInput.invalid && descInput.touched" class="error-text">
                Please enter at least 10 characters describing the issue.
              </div>
            </div>

            <button 
              type="submit" 
              [disabled]="complaintForm.invalid || sending()" 
              class="gradient-btn submit-btn"
            >
              <span *ngIf="sending()">Submitting Report...</span>
              <span *ngIf="!sending()">Submit Report</span>
            </button>
          </form>
        </section>

        <!-- Past Complaints List -->
        <section class="list-section">
          <h2>My Submitted Reports</h2>
          
          <div *ngIf="loading()" class="loading-state">
            <div class="spinner"></div>
            <p>Loading reports...</p>
          </div>

          <div *ngIf="!loading() && complaints().length === 0" class="empty-state glass-panel">
            <p>You haven't submitted any incident reports yet.</p>
          </div>

          <div class="complaints-list" *ngIf="!loading() && complaints().length > 0">
            <div 
              *ngFor="let item of complaints()" 
              class="complaint-card glass-panel"
            >
              <div class="card-header">
                <span class="badge" [class]="getBadgeClass(item.status)">{{ item.status }}</span>
                <span class="category-badge">{{ item.category }}</span>
                <span class="date">{{ item.createdAt | date:'shortDate' }}</span>
              </div>
              <p class="desc">{{ item.description }}</p>
              
              <!-- Resolution section if resolved -->
              <div class="resolution-panel" *ngIf="item.resolution">
                <h4>Resolution Note:</h4>
                <p>{{ item.resolution }}</p>
                <span class="resolved-by" *ngIf="item.updatedAt">
                  Resolved on {{ item.updatedAt | date:'mediumDate' }}
                </span>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .complaints-container {
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
    .complaints-header {
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
    .complaints-layout {
      display: grid;
      grid-template-columns: 360px 1fr;
      gap: 40px;
      width: 100%;
      max-width: 960px;
      align-items: start;
    }
    @media (max-width: 800px) {
      .complaints-layout {
        grid-template-columns: 1fr;
      }
    }
    .form-section {
      padding: 30px;
    }
    .form-section h2, .list-section h2 {
      font-size: 1.25rem;
      margin-bottom: 20px;
    }
    .submit-btn {
      width: 100%;
      padding: 12px;
    }
    .complaints-list {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .complaint-card {
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
    }
    .category-badge {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--text-secondary);
      background: rgba(255,255,255,0.05);
      padding: 2px 8px;
      border-radius: 4px;
    }
    .date {
      margin-left: auto;
      font-size: 0.8rem;
      color: var(--text-muted);
    }
    .desc {
      font-size: 0.92rem;
      color: var(--text-primary);
      white-space: pre-wrap;
    }
    .resolution-panel {
      background: rgba(0, 242, 254, 0.04);
      border-left: 3px solid var(--accent);
      padding: 12px 16px;
      border-radius: 4px;
      font-size: 0.88rem;
      margin-top: 8px;
    }
    .resolution-panel h4 {
      font-size: 0.9rem;
      margin-bottom: 4px;
    }
    .resolved-by {
      display: block;
      margin-top: 6px;
      font-size: 0.75rem;
      color: var(--text-muted);
    }
    .empty-state {
      padding: 30px;
      text-align: center;
      color: var(--text-muted);
    }
    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 40px;
      gap: 12px;
    }
    .spinner {
      border: 3px solid rgba(255,255,255,0.1);
      width: 30px;
      height: 30px;
      border-radius: 50%;
      border-left-color: var(--primary);
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `]
})
export class ComplaintsComponent implements OnInit {
  complaints = signal<Complaint[]>([]);
  loading = signal(true);
  sending = signal(false);

  // Form
  category = 'Harassment';
  description = '';

  constructor(private complaintService: ComplaintService) {}

  ngOnInit(): void {
    this.loadComplaints();
  }

  loadComplaints(): void {
    this.loading.set(true);
    this.complaintService.getMyComplaints().subscribe({
      next: (data) => {
        this.complaints.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSubmit(): void {
    if (!this.description.trim() || this.sending()) return;
    this.sending.set(true);

    this.complaintService.submitComplaint({
      category: this.category,
      description: this.description
    }).subscribe({
      next: () => {
        this.sending.set(false);
        this.description = '';
        this.loadComplaints();
        alert('Your support ticket was submitted successfully.');
      },
      error: (err) => {
        this.sending.set(false);
        alert(err.error?.error || 'Failed to submit support ticket.');
      }
    });
  }

  getBadgeClass(status: string): string {
    switch (status) {
      case 'Open': return 'badge-primary';
      case 'UnderReview': return 'badge-warning';
      case 'Resolved': return 'badge-success';
      case 'Dismissed': return 'badge-danger';
      default: return 'badge-primary';
    }
  }
}
