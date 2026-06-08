import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Workspace, WorkspaceMember, WorkspaceInvitation } from '../models';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private readonly baseUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  getMyWorkspaces(): Observable<Workspace[]> {
    return this.http.get<Workspace[]>(`${this.baseUrl}/workspaces`);
  }

  createWorkspace(request: { name: string; description?: string; logoUrl?: string }): Observable<Workspace> {
    return this.http.post<Workspace>(`${this.baseUrl}/workspaces`, request);
  }

  getWorkspace(id: string): Observable<Workspace> {
    return this.http.get<Workspace>(`${this.baseUrl}/workspaces/${id}`);
  }

  updateWorkspace(id: string, request: { name: string; description?: string; logoUrl?: string }): Observable<Workspace> {
    return this.http.post<Workspace>(`${this.baseUrl}/workspaces/${id}`, request);
  }

  deleteWorkspace(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/workspaces/${id}`);
  }

  getMembers(workspaceId: string): Observable<WorkspaceMember[]> {
    return this.http.get<WorkspaceMember[]>(`${this.baseUrl}/workspaces/${workspaceId}/members`);
  }

  removeMember(workspaceId: string, userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/workspaces/${workspaceId}/members/${userId}`);
  }

  updateMemberRole(workspaceId: string, userId: string, role: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/workspaces/${workspaceId}/members/${userId}/role`, { role });
  }

  sendInvitation(workspaceId: string, email: string, role: string): Observable<WorkspaceInvitation> {
    return this.http.post<WorkspaceInvitation>(`${this.baseUrl}/workspaces/${workspaceId}/invitations`, { email, role });
  }

  getInvitations(workspaceId: string): Observable<WorkspaceInvitation[]> {
    return this.http.get<WorkspaceInvitation[]>(`${this.baseUrl}/workspaces/${workspaceId}/invitations`);
  }

  revokeInvitation(workspaceId: string, invitationId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/workspaces/${workspaceId}/invitations/${invitationId}`);
  }

  acceptInvitation(token: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/invitations/accept`, null, { params: { token } });
  }

  declineInvitation(token: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/invitations/decline`, null, { params: { token } });
  }
}
