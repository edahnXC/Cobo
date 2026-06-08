import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AdminService } from '../core/admin.service';
import { ComplaintService } from '../core/complaint.service';
import { User, Complaint } from '../models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css'
})
export class AdminComponent implements OnInit {
  activeTab = signal<'users' | 'complaints' | 'audit' | 'email'>('users');
  loading = signal(true);

  // Tab - Users
  users = signal<User[]>([]);
  userSearch = '';
  usersPage = 1;
  usersTotal = 0;

  // Tab - Complaints
  complaints = signal<Complaint[]>([]);
  complaintsPage = 1;
  complaintsTotal = 0;
  selectedComplaintForResolve = signal<Complaint | null>(null);
  resolutionText = '';
  resolving = signal(false);

  // Tab - Audit Logs
  auditLogs = signal<any[]>([]);
  auditPage = 1;
  auditTotal = 0;
  auditActionFilter = '';

  // Tab - Email Logs
  emailLogs = signal<any[]>([]);
  emailPage = 1;
  emailTotal = 0;
  emailStatusFilter = '';

  pageSize = 20;

  constructor(
    private adminService: AdminService,
    private complaintService: ComplaintService
  ) {}

  ngOnInit(): void {
    this.switchTab('users');
  }

  switchTab(tab: 'users' | 'complaints' | 'audit' | 'email'): void {
    this.activeTab.set(tab);
    this.loading.set(true);
    
    if (tab === 'users') {
      this.loadUsers();
    } else if (tab === 'complaints') {
      this.loadComplaints();
    } else if (tab === 'audit') {
      this.loadAuditLogs();
    } else if (tab === 'email') {
      this.loadEmailLogs();
    }
  }

  // ─── User Management ────────────────────────────────────────────────────────

  loadUsers(): void {
    this.adminService.getUsers(this.usersPage, this.pageSize, this.userSearch || undefined).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.usersTotal = res.totalCount;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onUserSearch(): void {
    this.usersPage = 1;
    this.loadUsers();
  }

  toggleUserStatus(user: User): void {
    const nextStatus = !user.isActive;
    this.adminService.updateUserStatus(user.userId, nextStatus).subscribe({
      next: () => {
        this.users.update(list => list.map(u => u.userId === user.userId ? { ...u, isActive: nextStatus } : u));
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to update user status.');
      }
    });
  }

  // ─── Complaint Management ──────────────────────────────────────────────────

  loadComplaints(): void {
    this.complaintService.getAllComplaints(this.complaintsPage, this.pageSize).subscribe({
      next: (res) => {
        this.complaints.set(res.items);
        this.complaintsTotal = res.totalCount;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  openResolveModal(comp: Complaint): void {
    this.resolutionText = '';
    this.selectedComplaintForResolve.set(comp);
  }

  closeResolveModal(): void {
    this.selectedComplaintForResolve.set(null);
  }

  submitResolution(): void {
    const comp = this.selectedComplaintForResolve();
    if (!comp || !this.resolutionText.trim() || this.resolving()) return;

    this.resolving.set(true);
    this.complaintService.resolveComplaint(comp.complaintId, this.resolutionText.trim()).subscribe({
      next: (updated) => {
        this.resolving.set(false);
        this.selectedComplaintForResolve.set(null);
        this.complaints.update(list => list.map(c => c.complaintId === comp.complaintId ? updated : c));
        alert('Complaint marked as resolved. Complainant has been notified via email.');
      },
      error: (err) => {
        this.resolving.set(false);
        alert(err.error?.error || 'Failed to resolve complaint.');
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

  // ─── Audit Logs ────────────────────────────────────────────────────────────

  loadAuditLogs(): void {
    this.adminService.getAuditLogs(this.auditPage, this.pageSize, this.auditActionFilter || undefined).subscribe({
      next: (res) => {
        this.auditLogs.set(res.items);
        this.auditTotal = res.totalCount;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onAuditFilter(): void {
    this.auditPage = 1;
    this.loadAuditLogs();
  }

  // ─── Email Logs ─────────────────────────────────────────────────────────────

  loadEmailLogs(): void {
    this.adminService.getEmailLogs(this.emailPage, this.pageSize, this.emailStatusFilter || undefined).subscribe({
      next: (res) => {
        this.emailLogs.set(res.items);
        this.emailTotal = res.totalCount;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onEmailFilter(): void {
    this.emailPage = 1;
    this.loadEmailLogs();
  }

  // ─── Paginated Helpers ──────────────────────────────────────────────────────

  getTotalPages(total: number): number {
    return Math.ceil(total / this.pageSize) || 1;
  }

  prevPage(type: 'users' | 'complaints' | 'audit' | 'email'): void {
    if (type === 'users' && this.usersPage > 1) {
      this.usersPage--;
      this.loadUsers();
    } else if (type === 'complaints' && this.complaintsPage > 1) {
      this.complaintsPage--;
      this.loadComplaints();
    } else if (type === 'audit' && this.auditPage > 1) {
      this.auditPage--;
      this.loadAuditLogs();
    } else if (type === 'email' && this.emailPage > 1) {
      this.emailPage--;
      this.loadEmailLogs();
    }
  }

  nextPage(type: 'users' | 'complaints' | 'audit' | 'email', total: number): void {
    const totalPages = this.getTotalPages(total);
    if (type === 'users' && this.usersPage < totalPages) {
      this.usersPage++;
      this.loadUsers();
    } else if (type === 'complaints' && this.complaintsPage < totalPages) {
      this.complaintsPage++;
      this.loadComplaints();
    } else if (type === 'audit' && this.auditPage < totalPages) {
      this.auditPage++;
      this.loadAuditLogs();
    } else if (type === 'email' && this.emailPage < totalPages) {
      this.emailPage++;
      this.loadEmailLogs();
    }
  }
}
