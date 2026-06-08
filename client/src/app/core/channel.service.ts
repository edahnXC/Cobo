import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Channel } from '../models';

@Injectable({ providedIn: 'root' })
export class ChannelService {
  private readonly baseUrl = `${environment.apiUrl}/workspaces`;

  constructor(private http: HttpClient) {}

  getChannels(workspaceId: string): Observable<Channel[]> {
    return this.http.get<Channel[]>(`${this.baseUrl}/${workspaceId}/channels`);
  }

  createChannel(workspaceId: string, request: { channelName: string; channelType: string; isPrivate: boolean; isPremium: boolean }): Observable<Channel> {
    return this.http.post<Channel>(`${this.baseUrl}/${workspaceId}/channels`, request);
  }

  getChannel(workspaceId: string, channelId: string): Observable<Channel> {
    return this.http.get<Channel>(`${this.baseUrl}/${workspaceId}/channels/${channelId}`);
  }

  updateChannel(workspaceId: string, channelId: string, request: { channelName: string; isPrivate: boolean; isPremium: boolean; isArchived: boolean }): Observable<Channel> {
    return this.http.put<Channel>(`${this.baseUrl}/${workspaceId}/channels/${channelId}`, request);
  }

  deleteChannel(workspaceId: string, channelId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${workspaceId}/channels/${channelId}`);
  }

  addChannelMember(workspaceId: string, channelId: string, userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${workspaceId}/channels/${channelId}/members`, { userId });
  }

  removeChannelMember(workspaceId: string, channelId: string, userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${workspaceId}/channels/${channelId}/members/${userId}`);
  }
}
