import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthStateService } from './auth-state.service';
import { Message, MessagePage, ReactionSummary } from '../models';

@Injectable({ providedIn: 'root' })
export class MessageService {
  private readonly baseUrl = `${environment.apiUrl}/channels`;
  private hubConnection: HubConnection | null = null;

  readonly messageReceived$ = new Subject<Message>();
  readonly messageEdited$ = new Subject<Message>();
  readonly messageDeleted$ = new Subject<string>();
  readonly reactionAdded$ = new Subject<{ messageId: string; userId: string; emoji: string }>();
  readonly reactionRemoved$ = new Subject<{ messageId: string; userId: string; emoji: string }>();
  readonly gameEvent$ = new Subject<{ eventType: string; payload: string }>();

  // Connection status signal
  readonly isConnected = signal(false);

  constructor(private http: HttpClient, private authState: AuthStateService) {}

  // ─── HTTP endpoints ──────────────────────────────────────────────────────────

  getMessages(channelId: string, before?: string, pageSize = 50): Observable<MessagePage> {
    const params: any = { pageSize };
    if (before) params.before = before;
    return this.http.get<MessagePage>(`${this.baseUrl}/${channelId}/messages`, { params });
  }

  sendMessage(channelId: string, content: string, parentMessageId?: string): Observable<Message> {
    return this.http.post<Message>(`${this.baseUrl}/${channelId}/messages`, { content, parentMessageId });
  }

  editMessage(channelId: string, messageId: string, content: string): Observable<Message> {
    return this.http.put<Message>(`${this.baseUrl}/${channelId}/messages/${messageId}`, { content });
  }

  deleteMessage(channelId: string, messageId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${channelId}/messages/${messageId}`);
  }

  addReaction(channelId: string, messageId: string, emoji: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${channelId}/messages/${messageId}/reactions`, { emoji });
  }

  removeReaction(channelId: string, messageId: string, emoji: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${channelId}/messages/${messageId}/reactions/${encodeURIComponent(emoji)}`);
  }

  // ─── SignalR Hub Integration ───────────────────────────────────────────────

  async startHubConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    const hubUrl = `${environment.apiUrl.replace('/api/v1', '')}/hubs/chat`;
    
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.authState.accessToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.registerHubListeners();

    try {
      await this.hubConnection.start();
      this.isConnected.set(true);
      console.log('SignalR connection established.');
    } catch (err) {
      console.error('Error starting SignalR connection:', err);
      this.isConnected.set(false);
    }

    this.hubConnection.onclose(() => this.isConnected.set(false));
    this.hubConnection.onreconnected(() => this.isConnected.set(true));
    this.hubConnection.onreconnecting(() => this.isConnected.set(false));
  }

  async stopHubConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      await this.hubConnection.stop();
      this.isConnected.set(false);
      console.log('SignalR connection stopped.');
    }
  }

  async joinChannelGroup(channelId: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinChannel', channelId);
    }
  }

  async leaveChannelGroup(channelId: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveChannel', channelId);
    }
  }

  async sendGameEvent(channelId: string, eventType: string, payload: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendGameEvent', channelId, eventType, payload);
    }
  }

  private registerHubListeners(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMessage', (message: Message) => {
      this.messageReceived$.next(message);
    });

    this.hubConnection.on('MessageEdited', (message: Message) => {
      this.messageEdited$.next(message);
    });

    this.hubConnection.on('MessageDeleted', (messageId: string) => {
      this.messageDeleted$.next(messageId);
    });

    this.hubConnection.on('ReactionAdded', (messageId: string, userId: string, emoji: string) => {
      this.reactionAdded$.next({ messageId, userId, emoji });
    });

    this.hubConnection.on('ReactionRemoved', (messageId: string, userId: string, emoji: string) => {
      this.reactionRemoved$.next({ messageId, userId, emoji });
    });

    this.hubConnection.on('GameEventReceived', (eventType: string, payload: string) => {
      this.gameEvent$.next({ eventType, payload });
    });
  }
}
