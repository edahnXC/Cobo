import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { WorkspaceService } from '../core/workspace.service';
import { AuthStateService } from '../core/auth-state.service';
import { AuthService } from '../core/auth.service';
import { Workspace } from '../models';

@Component({
  selector: 'app-workspace-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="dashboard-container">
      <header class="dashboard-header glass-panel">
        <div class="logo-area">
          <span class="logo-icon">🔮</span>
          <h1 class="gradient-text">Cobo Platform</h1>
        </div>
        <div class="user-profile">
          <span class="user-name">{{ authState.user()?.fullName }}</span>
          <span class="badge badge-primary" *ngIf="authState.isPlatformAdmin()">Admin</span>
          <span class="badge badge-success" *ngIf="authState.isPremium()">Premium</span>
          
          <button (click)="logout()" class="logout-btn">Logout</button>
        </div>
      </header>

      <main class="dashboard-content">
        <div class="workspaces-section">
          <div class="section-header">
            <h2>Your Workspaces</h2>
            <button (click)="openCreateModal()" class="gradient-btn create-btn">+ Create Workspace</button>
          </div>

          <div *ngIf="loading()" class="loading-state">
            <div class="spinner"></div>
            <p>Loading workspaces...</p>
          </div>

          <div *ngIf="!loading() && workspaces().length === 0" class="empty-state glass-panel">
            <span class="empty-icon">📂</span>
            <h3>No Workspaces Yet</h3>
            <p>Workspaces are invite-only. Create one yourself or ask your team owner for an email invitation.</p>
            <button (click)="openCreateModal()" class="gradient-btn" style="margin-top: 15px;">Get Started</button>
          </div>

          <div *ngIf="!loading() && workspaces().length > 0" class="workspaces-grid">
            <div 
              *ngFor="let ws of workspaces()" 
              (click)="goToWorkspace(ws.workspaceId)"
              class="workspace-card glass-panel hover-grow"
            >
              <div class="ws-logo" [style.background-color]="getRandomColor(ws.name)">
                {{ ws.name.substring(0, 2).toUpperCase() }}
              </div>
              <div class="ws-info">
                <h3>{{ ws.name }}</h3>
                <p class="ws-desc">{{ ws.description || 'No description provided.' }}</p>
                <div class="ws-meta">
                  <span class="meta-item">👥 {{ ws.memberCount }} members</span>
                  <span class="meta-item">👑 Owned by {{ ws.ownerName }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div class="side-options">
          <div class="glass-panel quick-links">
            <h3>Quick Actions</h3>
            <a routerLink="/plans" class="quick-link-item hover-grow">
              <span>💳 Pricing & Premium Tiers</span>
              <span class="arrow">→</span>
            </a>
            <a routerLink="/complaints" class="quick-link-item hover-grow">
              <span>⚠️ Submit / View Complaints</span>
              <span class="arrow">→</span>
            </a>
            <a *ngIf="authState.isPlatformAdmin()" routerLink="/admin" class="quick-link-item hover-grow admin-link">
              <span>⚙️ Platform Administration</span>
              <span class="arrow">→</span>
            </a>
          </div>

          <div class="glass-panel theme-panel">
            <h3>Theme Preferences</h3>
            <button (click)="toggleDarkMode()" class="btn-secondary theme-toggle">
              {{ isDarkMode() ? '☀️ Switch to Light Mode' : '🌙 Switch to Dark Mode' }}
            </button>
          </div>
        </div>
      </main>

      <!-- Create Workspace Modal -->
      <div class="modal-backdrop" *ngIf="showCreateModal()">
        <div class="glass-panel modal-card">
          <div class="modal-header">
            <h3>Create a New Workspace</h3>
            <button (click)="closeCreateModal()" class="close-btn">&times;</button>
          </div>
          <form (ngSubmit)="onCreateWorkspace()">
            <div class="modal-body">
              <div class="form-group">
                <label class="form-label" for="wsName">Workspace Name</label>
                <input 
                  type="text" 
                  id="wsName" 
                  name="wsName" 
                  [(ngModel)]="newWorkspaceName" 
                  required 
                  class="form-input"
                  placeholder="e.g. Acme Tech, Engineering Group"
                />
              </div>
              <div class="form-group">
                <label class="form-label" for="wsDesc">Description (Optional)</label>
                <textarea 
                  id="wsDesc" 
                  name="wsDesc" 
                  [(ngModel)]="newWorkspaceDesc" 
                  class="form-input" 
                  rows="3"
                  placeholder="What is this workspace for?"
                ></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" (click)="closeCreateModal()" class="btn-secondary">Cancel</button>
              <button type="submit" [disabled]="!newWorkspaceName.trim()" class="gradient-btn">Create</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      min-height: 100vh;
      background: radial-gradient(circle at 0% 0%, rgba(98, 0, 234, 0.08) 0%, transparent 50%),
                  radial-gradient(circle at 100% 100%, rgba(0, 242, 254, 0.08) 0%, transparent 50%),
                  var(--bg-primary);
      padding: 30px;
    }
    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px 30px;
      margin-bottom: 30px;
    }
    .logo-area {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .logo-icon {
      font-size: 1.8rem;
    }
    .user-profile {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    .user-name {
      font-weight: 600;
    }
    .logout-btn {
      background: transparent;
      border: 1px solid var(--card-border);
      color: var(--text-muted);
      padding: 6px 12px;
      border-radius: var(--border-radius-sm);
      cursor: pointer;
      font-size: 0.85rem;
      transition: all var(--transition-fast);
    }
    .logout-btn:hover {
      border-color: var(--danger);
      color: var(--danger);
      background: rgba(255, 23, 68, 0.05);
    }
    .dashboard-content {
      display: grid;
      grid-template-columns: 1fr 320px;
      gap: 30px;
    }
    @media (max-width: 900px) {
      .dashboard-content {
        grid-template-columns: 1fr;
      }
    }
    .workspaces-section {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .workspaces-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }
    .workspace-card {
      padding: 20px;
      cursor: pointer;
      display: flex;
      align-items: flex-start;
      gap: 15px;
      height: 100%;
    }
    .ws-logo {
      width: 48px;
      height: 48px;
      border-radius: var(--border-radius-sm);
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: 800;
      font-size: 1.2rem;
      flex-shrink: 0;
    }
    .ws-info h3 {
      font-size: 1.1rem;
      margin-bottom: 4px;
    }
    .ws-desc {
      color: var(--text-muted);
      font-size: 0.88rem;
      margin-bottom: 12px;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .ws-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      font-size: 0.75rem;
      color: var(--text-secondary);
    }
    .meta-item {
      background: rgba(255, 255, 255, 0.05);
      padding: 2px 6px;
      border-radius: 4px;
    }
    body.light-mode .meta-item {
      background: rgba(0, 0, 0, 0.04);
    }
    .side-options {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .quick-links, .theme-panel {
      padding: 20px;
    }
    .quick-links h3, .theme-panel h3 {
      font-size: 1rem;
      margin-bottom: 15px;
      border-bottom: 1px solid var(--card-border);
      padding-bottom: 8px;
    }
    .quick-link-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 10px 12px;
      border-radius: var(--border-radius-sm);
      color: var(--text-primary);
      margin-bottom: 8px;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid transparent;
    }
    .quick-link-item:hover {
      background: rgba(255, 255, 255, 0.05);
      border-color: var(--card-border);
    }
    body.light-mode .quick-link-item {
      background: rgba(0, 0, 0, 0.01);
    }
    body.light-mode .quick-link-item:hover {
      background: rgba(0, 0, 0, 0.04);
    }
    .admin-link:hover {
      border-color: var(--primary);
    }
    .arrow {
      color: var(--accent);
      font-weight: bold;
    }
    .theme-toggle {
      width: 100%;
      padding: 10px;
      font-size: 0.9rem;
    }
    .empty-state {
      padding: 60px 40px;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
    }
    .empty-icon {
      font-size: 4rem;
    }
    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px;
      gap: 15px;
    }
    .spinner {
      border: 3px solid rgba(255,255,255,0.1);
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border-left-color: var(--primary);
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
    /* Modal styles */
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.6);
      backdrop-filter: blur(4px);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }
    .modal-card {
      width: 100%;
      max-width: 480px;
      padding: 30px;
      animation: modalSlide 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .close-btn {
      background: transparent;
      border: none;
      color: var(--text-muted);
      font-size: 1.8rem;
      cursor: pointer;
    }
    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      margin-top: 25px;
    }
    @keyframes modalSlide {
      from { opacity: 0; transform: scale(0.95) translateY(10px); }
      to { opacity: 1; transform: scale(1) translateY(0); }
    }
  `]
})
export class WorkspaceListComponent implements OnInit {
  workspaces = signal<Workspace[]>([]);
  loading = signal(true);
  isDarkMode = signal(true);

  // Modal State
  showCreateModal = signal(false);
  newWorkspaceName = '';
  newWorkspaceDesc = '';

  constructor(
    private workspaceService: WorkspaceService,
    private authService: AuthService,
    readonly authState: AuthStateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check local storage theme
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'light') {
      this.isDarkMode.set(false);
      document.body.classList.add('light-mode');
    }

    this.loadWorkspaces();
  }

  loadWorkspaces(): void {
    this.loading.set(true);
    this.workspaceService.getMyWorkspaces().subscribe({
      next: (data) => {
        this.workspaces.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  goToWorkspace(id: string): void {
    this.router.navigate(['/workspaces', id]);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  toggleDarkMode(): void {
    const current = this.isDarkMode();
    this.isDarkMode.set(!current);
    if (current) {
      document.body.classList.add('light-mode');
      localStorage.setItem('theme', 'light');
    } else {
      document.body.classList.remove('light-mode');
      localStorage.setItem('theme', 'dark');
    }
  }

  openCreateModal(): void {
    this.newWorkspaceName = '';
    this.newWorkspaceDesc = '';
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
  }

  onCreateWorkspace(): void {
    if (!this.newWorkspaceName.trim()) return;

    this.workspaceService.createWorkspace({
      name: this.newWorkspaceName,
      description: this.newWorkspaceDesc
    }).subscribe({
      next: (ws) => {
        this.showCreateModal.set(false);
        this.goToWorkspace(ws.workspaceId);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to create workspace. Try upgrading your subscription if you exceed plan limits.');
      }
    });
  }

  getRandomColor(name: string): string {
    const hash = name.split('').reduce((acc, char) => char.charCodeAt(0) + ((acc << 5) - acc), 0);
    const colors = ['#6200ea', '#00b0ff', '#00e676', '#ff1744', '#ff8f00', '#f50057', '#d500f9', '#00e5ff'];
    return colors[Math.abs(hash) % colors.length];
  }
}
