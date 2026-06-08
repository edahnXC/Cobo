import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Complaint, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class ComplaintService {
  private readonly baseUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  submitComplaint(request: { reportedUserId?: string; workspaceId?: string; messageId?: string; category: string; description: string }): Observable<Complaint> {
    return this.http.post<Complaint>(`${this.baseUrl}/complaints`, request);
  }

  getMyComplaints(): Observable<Complaint[]> {
    return this.http.get<Complaint[]>(`${this.baseUrl}/complaints/mine`);
  }

  getAllComplaints(page = 1, pageSize = 20, status?: string): Observable<PagedResult<Complaint>> {
    const params: any = { page, pageSize };
    if (status) params.status = status;
    return this.http.get<PagedResult<Complaint>>(`${this.baseUrl}/admin/complaints`, { params });
  }

  resolveComplaint(id: string, resolution: string): Observable<Complaint> {
    return this.http.put<Complaint>(`${this.baseUrl}/admin/complaints/${id}`, { resolution });
  }
}
