import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult, User } from '../models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getUsers(page = 1, pageSize = 20, search?: string): Observable<PagedResult<User>> {
    const params: any = { page, pageSize };
    if (search) params.search = search;
    return this.http.get<PagedResult<User>>(`${this.baseUrl}/users`, { params });
  }

  updateUserStatus(userId: string, isActive: boolean): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/users/${userId}/status`, { isActive });
  }

  getAuditLogs(page = 1, pageSize = 20, action?: string, userId?: string): Observable<PagedResult<any>> {
    const params: any = { page, pageSize };
    if (action) params.action = action;
    if (userId) params.userId = userId;
    return this.http.get<PagedResult<any>>(`${this.baseUrl}/audit-logs`, { params });
  }

  getEmailLogs(page = 1, pageSize = 20, status?: string): Observable<PagedResult<any>> {
    const params: any = { page, pageSize };
    if (status) params.status = status;
    return this.http.get<PagedResult<any>>(`${this.baseUrl}/email-logs`, { params });
  }
}
