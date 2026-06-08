import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild, HostListener } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { AuthStateService } from '../core/auth-state.service';
import { WorkspaceService } from '../core/workspace.service';
import { ChannelService } from '../core/channel.service';
import { MessageService } from '../core/message.service';
import { SubscriptionService } from '../core/subscription.service';
import { ComplaintService } from '../core/complaint.service';
import { Workspace, Channel, Message, WorkspaceMember, WorkspaceInvitation } from '../models';

interface VoiceParticipant {
  userId: string;
  fullName: string;
  avatarUrl?: string;
  isMuted: boolean;
  isSpeaking: boolean;
}

interface GamePlayer {
  userId: string;
  fullName: string;
  score: number;
  hasGuessed: boolean;
}

@Component({
  selector: 'app-workspace-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './workspace-detail.component.html',
  styleUrl: './workspace-detail.component.css'
})
export class WorkspaceDetailComponent implements OnInit, OnDestroy {
  @ViewChild('messageLog') private messageLog!: ElementRef;
  @ViewChild('gameCanvas') private gameCanvas!: ElementRef<HTMLCanvasElement>;

  // Route & Core IDs
  workspaceId = '';
  activeChannelId = signal<string | null>(null);

  // Loaded Data
  workspace = signal<Workspace | null>(null);
  channels = signal<Channel[]>([]);
  activeChannel = signal<Channel | null>(null);
  messages = signal<Message[]>([]);
  members = signal<WorkspaceMember[]>([]);
  invitations = signal<WorkspaceInvitation[]>([]);

  // Member Role permissions
  myRole = signal<string>('Member');
  isOwnerOrMod = signal(false);

  // Loading states
  loadingWorkspace = signal(true);
  loadingChannels = signal(true);
  loadingMessages = signal(false);
  hasMoreMessages = signal(false);
  nextCursor = signal<string | null>(null);

  // Message Send/Edit states
  newMsgContent = '';
  editingMessageId = signal<string | null>(null);
  editedMsgContent = '';

  // Modals & Popups
  showSettingsDropdown = signal(false);
  showCreateChannelModal = signal(false);
  showInviteModal = signal(false);
  showMembersModal = signal(false);
  showInvitesModal = signal(false);
  showEmojiPickerForMsgId = signal<string | null>(null);
  showReportModal = signal(false);

  // Form Inputs
  newChannelName = '';
  newChannelType = 'text';
  newChannelIsPrivate = false;
  newChannelIsPremium = false;
  
  inviteEmail = '';
  inviteRole = 'Member';

  // Report Form Inputs
  reportCategory = 'Harassment';
  reportDescription = '';
  reportMsgId = signal<string | null>(null);
  reportUserId = signal<string | null>(null);
  reportWsId = signal<string | null>(null);

  // ─── Voice Channel Call State ──────────────────────────────────────────────
  inVoiceCall = signal(false);
  isMicMuted = signal(false);
  voiceParticipants = signal<VoiceParticipant[]>([]);
  private voiceSpeakerTimer: any = null;

  // ─── Game Channel Room State ───────────────────────────────────────────────
  inGameRoom = signal(false);
  gamePlayers = signal<GamePlayer[]>([]);
  gameDrawerId = signal<string | null>(null);
  gameTargetWord = signal<string>('');
  gameWordHint = signal<string>('');
  gameTimeLeft = signal<number>(0);
  gameStatusText = signal<string>('Waiting for players to start...');
  gameIsActive = signal(false);
  gameLogs = signal<string[]>([]);
  gameWordsToSelect = signal<string[]>([]);
  isDrawingTurn = signal(false);
  canvasColor = '#f5f6f9';
  brushSize = 4;
  
  private gameTimerInterval: any = null;
  private isDrawing = false;
  private lastX = 0;
  private lastY = 0;

  // RxJS Subscriptions
  private rxSubs = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    readonly authState: AuthStateService,
    private workspaceService: WorkspaceService,
    private channelService: ChannelService,
    readonly messageService: MessageService,
    private subscriptionService: SubscriptionService,
    private complaintService: ComplaintService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('wId');
      if (id) {
        this.workspaceId = id;
        this.initWorkspace();
      }
    });

    this.messageService.startHubConnection();

    // Register SignalR listeners
    this.rxSubs.add(this.messageService.messageReceived$.subscribe(msg => {
      if (msg.channelId === this.activeChannelId()) {
        this.messages.update(msgs => [msg, ...msgs]);
        this.scrollToBottom();
      }
    }));

    this.rxSubs.add(this.messageService.messageEdited$.subscribe(msg => {
      if (msg.channelId === this.activeChannelId()) {
        this.messages.update(msgs => msgs.map(m => m.messageId === msg.messageId ? msg : m));
      }
    }));

    this.rxSubs.add(this.messageService.messageDeleted$.subscribe(mId => {
      this.messages.update(msgs => msgs.map(m => m.messageId === mId ? { ...m, isDeleted: true, content: '[Message deleted]' } : m));
    }));

    this.rxSubs.add(this.messageService.reactionAdded$.subscribe(react => {
      this.updateMessageReactions(react.messageId, react.userId, react.emoji, true);
    }));

    this.rxSubs.add(this.messageService.reactionRemoved$.subscribe(react => {
      this.updateMessageReactions(react.messageId, react.userId, react.emoji, false);
    }));

    this.rxSubs.add(this.messageService.gameEvent$.subscribe(event => {
      this.handleIncomingGameEvent(event.eventType, event.payload);
    }));
  }

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
    this.rxSubs.unsubscribe();
    if (this.voiceSpeakerTimer) clearInterval(this.voiceSpeakerTimer);
    if (this.gameTimerInterval) clearInterval(this.gameTimerInterval);
  }

  // ─── Workspace and Channel Init ──────────────────────────────────────────

  initWorkspace(): void {
    this.loadingWorkspace.set(true);
    this.loadingChannels.set(true);

    this.workspaceService.getWorkspace(this.workspaceId).subscribe({
      next: (ws) => {
        this.workspace.set(ws);
        this.loadingWorkspace.set(false);
        
        // Find my membership role
        this.workspaceService.getMembers(this.workspaceId).subscribe(members => {
          this.members.set(members);
          const me = members.find(m => m.userId === this.authState.user()?.userId);
          if (me) {
            this.myRole.set(me.memberRole);
            this.isOwnerOrMod.set(me.memberRole === 'Owner' || me.memberRole === 'Moderator');
          }
        });

        // Load channels
        this.channelService.getChannels(this.workspaceId).subscribe({
          next: (channels) => {
            this.channels.set(channels);
            this.loadingChannels.set(false);
            
            // Set first channel as active if exists and none selected
            if (channels.length > 0 && !this.activeChannelId()) {
              this.selectChannel(channels[0]);
            }
          },
          error: () => this.loadingChannels.set(false)
        });
      },
      error: () => {
        this.loadingWorkspace.set(false);
        this.router.navigate(['/workspaces']);
      }
    });
  }

  selectChannel(channel: Channel): void {
    if (this.activeChannelId() === channel.channelId) return;

    // Leave old group if connected
    if (this.activeChannelId()) {
      this.messageService.leaveChannelGroup(this.activeChannelId()!);
      this.cleanupVoiceAndGame();
    }

    this.activeChannelId.set(channel.channelId);
    this.activeChannel.set(channel);
    
    // Join new hub group
    this.messageService.joinChannelGroup(channel.channelId);

    // If channel is premium and user is not premium, we do not load messages/details
    if (channel.isPremium && !this.authState.isPremium()) {
      this.messages.set([]);
      return;
    }

    if (channel.channelType === 'text') {
      this.loadChannelMessages(channel.channelId);
    } else if (channel.channelType === 'voice') {
      this.initVoiceChannelState();
    } else if (channel.channelType === 'game') {
      this.initGameChannelState();
    }
  }

  // ─── Messaging ─────────────────────────────────────────────────────────────

  loadChannelMessages(channelId: string, before?: string): void {
    if (this.loadingMessages()) return;
    this.loadingMessages.set(true);

    this.messageService.getMessages(channelId, before).subscribe({
      next: (page) => {
        if (before) {
          this.messages.update(msgs => [...msgs, ...page.messages]);
        } else {
          this.messages.set(page.messages);
          this.scrollToBottom(true);
        }
        this.nextCursor.set(page.nextCursor || null);
        this.hasMoreMessages.set(page.hasMore);
        this.loadingMessages.set(false);
      },
      error: () => this.loadingMessages.set(false)
    });
  }

  onScroll(event: any): void {
    const element = event.target;
    if (element.scrollTop === 0 && this.hasMoreMessages() && this.nextCursor()) {
      this.loadChannelMessages(this.workspaceId, this.nextCursor()!);
    }
  }

  sendChatMessage(): void {
    if (!this.newMsgContent.trim() || !this.activeChannelId()) return;

    const content = this.newMsgContent;
    this.newMsgContent = ''; // clear early for responsiveness

    this.messageService.sendMessage(this.activeChannelId()!, content).subscribe({
      error: (err) => {
        alert(err.error?.error || 'Failed to send message.');
      }
    });
  }

  startEditing(msg: Message): void {
    if (msg.senderId !== this.authState.user()?.userId) return;
    this.editingMessageId.set(msg.messageId);
    this.editedMsgContent = msg.content;
  }

  saveEdit(): void {
    const mId = this.editingMessageId();
    if (!mId || !this.editedMsgContent.trim() || !this.activeChannelId()) return;

    this.messageService.editMessage(this.activeChannelId()!, mId, this.editedMsgContent).subscribe({
      next: () => {
        this.editingMessageId.set(null);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to edit message.');
      }
    });
  }

  cancelEdit(): void {
    this.editingMessageId.set(null);
  }

  deleteMessage(msgId: string): void {
    if (!confirm('Are you sure you want to delete this message? This action will replace the content with a soft-deleted placeholder.')) return;
    this.messageService.deleteMessage(this.activeChannelId()!, msgId).subscribe({
      error: (err) => {
        alert(err.error?.error || 'Failed to delete message.');
      }
    });
  }

  toggleEmojiPicker(msgId: string): void {
    this.showEmojiPickerForMsgId.set(this.showEmojiPickerForMsgId() === msgId ? null : msgId);
  }

  reactToMessage(msgId: string, emoji: string): void {
    this.showEmojiPickerForMsgId.set(null);
    const msg = this.messages().find(m => m.messageId === msgId);
    if (!msg) return;

    const existing = msg.reactions.find(r => r.emoji === emoji);
    if (existing && existing.reactedByMe) {
      this.messageService.removeReaction(this.activeChannelId()!, msgId, emoji).subscribe();
    } else {
      this.messageService.addReaction(this.activeChannelId()!, msgId, emoji).subscribe();
    }
  }

  private updateMessageReactions(messageId: string, userId: string, emoji: string, isAdd: boolean): void {
    const isMe = userId === this.authState.user()?.userId;
    this.messages.update(msgs => msgs.map(m => {
      if (m.messageId !== messageId) return m;

      let reactions = [...m.reactions];
      const existing = reactions.find(r => r.emoji === emoji);

      if (isAdd) {
        if (existing) {
          existing.count++;
          if (isMe) existing.reactedByMe = true;
        } else {
          reactions.push({ emoji, count: 1, reactedByMe: isMe });
        }
      } else {
        if (existing) {
          existing.count--;
          if (isMe) existing.reactedByMe = false;
          if (existing.count <= 0) {
            reactions = reactions.filter(r => r.emoji !== emoji);
          }
        }
      }

      return { ...m, reactions };
    }));
  }

  scrollToBottom(immediate = false): void {
    setTimeout(() => {
      try {
        const el = this.messageLog.nativeElement;
        el.scrollTo({
          top: el.scrollHeight,
          behavior: immediate ? 'auto' : 'smooth'
        });
      } catch (err) {}
    }, 50);
  }

  // ─── Voice Channels ────────────────────────────────────────────────────────

  initVoiceChannelState(): void {
    // Generate simulated participants including current user if they click join
    this.voiceParticipants.set([
      { userId: '1', fullName: 'Simulated Alex', isMuted: false, isSpeaking: false },
      { userId: '2', fullName: 'Simulated Sam', isMuted: true, isSpeaking: false }
    ]);
  }

  joinVoiceCall(): void {
    if (this.inVoiceCall()) return;
    this.inVoiceCall.set(true);
    
    // Add current user
    const user = this.authState.user();
    if (user) {
      this.voiceParticipants.update(parts => [
        ...parts,
        { userId: user.userId, fullName: user.fullName, avatarUrl: user.avatarUrl, isMuted: this.isMicMuted(), isSpeaking: false }
      ]);
    }

    // Broadcast voice join event
    this.messageService.sendGameEvent(this.activeChannelId()!, 'VOICE_JOIN', JSON.stringify({
      userId: user?.userId,
      fullName: user?.fullName
    }));

    // Start random speaking simulation for other simulated users to show premium glowing animations!
    this.voiceSpeakerTimer = setInterval(() => {
      this.voiceParticipants.update(parts => parts.map(p => {
        if (p.userId === user?.userId) {
          return { ...p, isSpeaking: !p.isMuted && Math.random() > 0.6 };
        }
        return { ...p, isSpeaking: !p.isMuted && Math.random() > 0.4 };
      }));
    }, 1200);
  }

  toggleMic(): void {
    const newMute = !this.isMicMuted();
    this.isMicMuted.set(newMute);
    
    const user = this.authState.user();
    this.voiceParticipants.update(parts => parts.map(p => 
      p.userId === user?.userId ? { ...p, isMuted: newMute, isSpeaking: false } : p
    ));

    this.messageService.sendGameEvent(this.activeChannelId()!, 'VOICE_MUTE_TOGGLE', JSON.stringify({
      userId: user?.userId,
      isMuted: newMute
    }));
  }

  leaveVoiceCall(): void {
    if (!this.inVoiceCall()) return;
    this.inVoiceCall.set(false);
    
    const user = this.authState.user();
    this.voiceParticipants.update(parts => parts.filter(p => p.userId !== user?.userId));
    if (this.voiceSpeakerTimer) clearInterval(this.voiceSpeakerTimer);

    this.messageService.sendGameEvent(this.activeChannelId()!, 'VOICE_LEAVE', JSON.stringify({
      userId: user?.userId
    }));
  }

  // ─── Game Channels (Scribble Game Room) ────────────────────────────────────

  initGameChannelState(): void {
    this.gamePlayers.set([
      { userId: '1', fullName: 'Simulated Alex', score: 120, hasGuessed: false },
      { userId: '2', fullName: 'Simulated Sam', score: 85, hasGuessed: false }
    ]);
  }

  startGame(): void {
    if (this.gameIsActive()) return;
    this.inGameRoom.set(true);
    this.gameIsActive.set(true);

    const user = this.authState.user();
    if (user && !this.gamePlayers().some(p => p.userId === user.userId)) {
      this.gamePlayers.update(players => [...players, { userId: user.userId, fullName: user.fullName, score: 0, hasGuessed: false }]);
    }

    this.gameLogs.set(['Game started! Waiting for drawer...']);
    
    // Broadcast game start
    this.messageService.sendGameEvent(this.activeChannelId()!, 'GAME_START', JSON.stringify({
      players: this.gamePlayers()
    }));

    // Start first round
    this.startNewRound(user?.userId || '1');
  }

  startNewRound(drawerId: string): void {
    this.gameDrawerId.set(drawerId);
    const isMe = drawerId === this.authState.user()?.userId;
    this.isDrawingTurn.set(isMe);
    this.gamePlayers.update(players => players.map(p => ({ ...p, hasGuessed: false })));

    if (isMe) {
      this.gameWordsToSelect.set(['Rocket', 'Banana', 'Castle', 'Pineapple', 'Rainbow']);
      this.gameStatusText.set('Choose a word to start drawing!');
    } else {
      const drawer = this.gamePlayers().find(p => p.userId === drawerId);
      this.gameStatusText.set(`${drawer?.fullName || 'Someone'} is choosing a word...`);
    }

    this.gameTimeLeft.set(60);
    this.clearCanvas();
  }

  selectGameWord(word: string): void {
    this.gameTargetWord.set(word.toLowerCase());
    this.gameWordsToSelect.set([]);
    
    // Create clue: e.g. "R _ _ _ _ _"
    const clue = word[0] + ' _'.repeat(word.length - 1);
    this.gameWordHint.set(clue);
    this.gameStatusText.set(`Draw the word: ${word.toUpperCase()}`);

    // Broadcast select word event
    this.messageService.sendGameEvent(this.activeChannelId()!, 'WORD_SELECTED', JSON.stringify({
      hint: clue,
      wordLength: word.length,
      drawerName: this.authState.user()?.fullName
    }));

    this.startTimer();
  }

  private startTimer(): void {
    if (this.gameTimerInterval) clearInterval(this.gameTimerInterval);
    this.gameTimerInterval = setInterval(() => {
      this.gameTimeLeft.update(t => {
        if (t <= 1) {
          clearInterval(this.gameTimerInterval);
          this.endRound();
          return 0;
        }
        return t - 1;
      });
    }, 1000);
  }

  submitGuess(guessInput: HTMLInputElement): void {
    const guess = guessInput.value.trim().toLowerCase();
    guessInput.value = '';
    if (!guess || this.isDrawingTurn() || !this.gameIsActive()) return;

    const user = this.authState.user();
    if (!user) return;

    if (guess === this.gameTargetWord().toLowerCase()) {
      // Correct guess!
      this.gamePlayers.update(players => players.map(p => {
        if (p.userId === user.userId && !p.hasGuessed) {
          this.gameLogs.update(logs => [...logs, `🎉 ${user.fullName} guessed correctly!`]);
          return { ...p, score: p.score + 100, hasGuessed: true };
        }
        return p;
      }));

      // Send SignalR event
      this.messageService.sendGameEvent(this.activeChannelId()!, 'CORRECT_GUESS', JSON.stringify({
        userId: user.userId,
        fullName: user.fullName
      }));

      // Check if all guessed
      const guessers = this.gamePlayers().filter(p => p.userId !== this.gameDrawerId());
      const allCorrect = guessers.every(p => p.hasGuessed);
      if (allCorrect) {
        this.endRound();
      }
    } else {
      // Wrong guess, send standard chat game log
      this.gameLogs.update(logs => [...logs, `${user.fullName}: ${guess}`]);
      this.messageService.sendGameEvent(this.activeChannelId()!, 'GAME_CHAT', JSON.stringify({
        senderName: user.fullName,
        text: guess
      }));
    }
  }

  endRound(): void {
    if (this.gameTimerInterval) clearInterval(this.gameTimerInterval);
    const word = this.gameTargetWord();
    this.gameLogs.update(logs => [...logs, `Round ended! The word was ${word.toUpperCase()}`]);
    this.gameStatusText.set(`Round ended! The word was ${word.toUpperCase()}`);

    // Cycle drawer
    setTimeout(() => {
      if (!this.gameIsActive()) return;
      const players = this.gamePlayers();
      const currentIdx = players.findIndex(p => p.userId === this.gameDrawerId());
      const nextIdx = (currentIdx + 1) % players.length;
      this.startNewRound(players[nextIdx].userId);
    }, 4000);
  }

  exitGame(): void {
    this.cleanupVoiceAndGame();
    this.messageService.sendGameEvent(this.activeChannelId()!, 'GAME_EXIT', JSON.stringify({
      userId: this.authState.user()?.userId
    }));
  }

  // ─── Drawing Canvas Logic ──────────────────────────────────────────────────

  onCanvasMouseDown(event: MouseEvent): void {
    if (!this.isDrawingTurn()) return;
    this.isDrawing = true;
    const canvas = this.gameCanvas.nativeElement;
    const rect = canvas.getBoundingClientRect();
    this.lastX = event.clientX - rect.left;
    this.lastY = event.clientY - rect.top;
  }

  onCanvasMouseMove(event: MouseEvent): void {
    if (!this.isDrawing || !this.isDrawingTurn()) return;
    const canvas = this.gameCanvas.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const rect = canvas.getBoundingClientRect();
    const currentX = event.clientX - rect.left;
    const currentY = event.clientY - rect.top;

    ctx.beginPath();
    ctx.moveTo(this.lastX, this.lastY);
    ctx.lineTo(currentX, currentY);
    ctx.strokeStyle = this.canvasColor;
    ctx.lineWidth = this.brushSize;
    ctx.lineCap = 'round';
    ctx.stroke();

    // Broadcast coords
    this.messageService.sendGameEvent(this.activeChannelId()!, 'DRAW', JSON.stringify({
      x0: this.lastX,
      y0: this.lastY,
      x1: currentX,
      y1: currentY,
      color: this.canvasColor,
      size: this.brushSize
    }));

    this.lastX = currentX;
    this.lastY = currentY;
  }

  @HostListener('document:mouseup')
  onCanvasMouseUp(): void {
    this.isDrawing = false;
  }

  clearCanvas(): void {
    const canvas = this.gameCanvas?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    ctx?.clearRect(0, 0, canvas.width, canvas.height);
  }

  private drawLineOnCanvas(x0: number, y0: number, x1: number, y1: number, color: string, size: number): void {
    const canvas = this.gameCanvas?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.beginPath();
    ctx.moveTo(x0, y0);
    ctx.lineTo(x1, y1);
    ctx.strokeStyle = color;
    ctx.lineWidth = size;
    ctx.lineCap = 'round';
    ctx.stroke();
  }

  // ─── Incoming Game & Voice Events Handler ───────────────────────────────

  private handleIncomingGameEvent(eventType: string, payloadStr: string): void {
    const payload = JSON.parse(payloadStr);
    switch (eventType) {
      case 'VOICE_JOIN':
        if (!this.voiceParticipants().some(p => p.userId === payload.userId)) {
          this.voiceParticipants.update(parts => [
            ...parts,
            { userId: payload.userId, fullName: payload.fullName, isMuted: false, isSpeaking: false }
          ]);
        }
        break;
      case 'VOICE_MUTE_TOGGLE':
        this.voiceParticipants.update(parts => parts.map(p => 
          p.userId === payload.userId ? { ...p, isMuted: payload.isMuted, isSpeaking: false } : p
        ));
        break;
      case 'VOICE_LEAVE':
        this.voiceParticipants.update(parts => parts.filter(p => p.userId !== payload.userId));
        break;
      case 'GAME_START':
        this.inGameRoom.set(true);
        this.gameIsActive.set(true);
        this.gamePlayers.set(payload.players);
        break;
      case 'WORD_SELECTED':
        this.gameWordHint.set(payload.hint);
        this.gameStatusText.set(`${payload.drawerName} is drawing...`);
        this.startTimer();
        break;
      case 'DRAW':
        this.drawLineOnCanvas(payload.x0, payload.y0, payload.x1, payload.y1, payload.color, payload.size);
        break;
      case 'CORRECT_GUESS':
        this.gamePlayers.update(players => players.map(p => 
          p.userId === payload.userId ? { ...p, score: p.score + 100, hasGuessed: true } : p
        ));
        this.gameLogs.update(logs => [...logs, `🎉 ${payload.fullName} guessed correctly!`]);
        break;
      case 'GAME_CHAT':
        this.gameLogs.update(logs => [...logs, `${payload.senderName}: ${payload.text}`]);
        break;
      case 'GAME_EXIT':
        this.gamePlayers.update(players => players.filter(p => p.userId !== payload.userId));
        break;
    }
  }

  private cleanupVoiceAndGame(): void {
    this.leaveVoiceCall();
    this.inVoiceCall.set(false);
    this.inGameRoom.set(false);
    this.gameIsActive.set(false);
    this.isDrawingTurn.set(false);
    if (this.gameTimerInterval) clearInterval(this.gameTimerInterval);
  }

  // ─── Modal actions, members management and workspace configs ───────────

  openCreateChannelModal(): void {
    this.newChannelName = '';
    this.newChannelType = 'text';
    this.newChannelIsPrivate = false;
    this.newChannelIsPremium = false;
    this.showCreateChannelModal.set(true);
  }

  onCreateChannel(): void {
    if (!this.newChannelName.trim()) return;
    this.channelService.createChannel(this.workspaceId, {
      channelName: this.newChannelName.trim(),
      channelType: this.newChannelType,
      isPrivate: this.newChannelIsPrivate,
      isPremium: this.newChannelIsPremium
    }).subscribe({
      next: (ch) => {
        this.channels.update(chs => [...chs, ch]);
        this.showCreateChannelModal.set(false);
        this.selectChannel(ch);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to create channel.');
      }
    });
  }

  deleteChannel(channelId: string, event: Event): void {
    event.stopPropagation();
    if (!confirm('Are you sure you want to delete/archive this channel?')) return;

    this.channelService.deleteChannel(this.workspaceId, channelId).subscribe({
      next: () => {
        this.channels.update(chs => chs.filter(ch => ch.channelId !== channelId));
        if (this.activeChannelId() === channelId) {
          const remaining = this.channels();
          if (remaining.length > 0) {
            this.selectChannel(remaining[0]);
          } else {
            this.activeChannelId.set(null);
            this.activeChannel.set(null);
          }
        }
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to delete channel.');
      }
    });
  }

  openInviteModal(): void {
    this.inviteEmail = '';
    this.inviteRole = 'Member';
    this.showInviteModal.set(true);
  }

  onSendInvite(): void {
    if (!this.inviteEmail.trim()) return;

    this.workspaceService.sendInvitation(this.workspaceId, this.inviteEmail.trim(), this.inviteRole).subscribe({
      next: () => {
        alert('Invitation sent successfully!');
        this.showInviteModal.set(false);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to send invitation.');
      }
    });
  }

  openMembersModal(): void {
    this.workspaceService.getMembers(this.workspaceId).subscribe(members => {
      this.members.set(members);
      this.showMembersModal.set(true);
    });
  }

  removeMember(userId: string): void {
    if (!confirm('Are you sure you want to remove this member from the workspace?')) return;
    this.workspaceService.removeMember(this.workspaceId, userId).subscribe({
      next: () => {
        this.members.update(mbs => mbs.filter(m => m.userId !== userId));
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to remove member.');
      }
    });
  }

  changeMemberRole(userId: string, newRole: string): void {
    this.workspaceService.updateMemberRole(this.workspaceId, userId, newRole).subscribe({
      next: () => {
        this.members.update(mbs => mbs.map(m => m.userId === userId ? { ...m, memberRole: newRole } : m));
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to update member role.');
      }
    });
  }

  openInvitesModal(): void {
    this.workspaceService.getInvitations(this.workspaceId).subscribe(invites => {
      this.invitations.set(invites);
      this.showInvitesModal.set(true);
    });
  }

  revokeInvitation(invitationId: string): void {
    if (!confirm('Are you sure you want to revoke this invitation?')) return;
    this.workspaceService.revokeInvitation(this.workspaceId, invitationId).subscribe({
      next: () => {
        this.invitations.update(invs => invs.filter(i => i.invitationId !== invitationId));
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to revoke invitation.');
      }
    });
  }

  onDeleteWorkspace(): void {
    if (!confirm('⚠️ WARNING: Are you sure you want to delete this workspace? This will delete all channels, messages, and invites. This action is irreversible!')) return;
    this.workspaceService.deleteWorkspace(this.workspaceId).subscribe({
      next: () => {
        this.router.navigate(['/workspaces']);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to delete workspace.');
      }
    });
  }

  // ─── Complaint Submission Modal ──────────────────────────────────────────

  openReportMsgModal(msg: Message): void {
    this.reportMsgId.set(msg.messageId);
    this.reportUserId.set(msg.senderId);
    this.reportWsId.set(this.workspaceId);
    this.reportCategory = 'Harassment';
    this.reportDescription = '';
    this.showReportModal.set(true);
  }

  openReportWsModal(): void {
    this.reportMsgId.set(null);
    this.reportUserId.set(null);
    this.reportWsId.set(this.workspaceId);
    this.reportCategory = 'InappropriateContent';
    this.reportDescription = '';
    this.showReportModal.set(true);
  }

  onSubmitComplaint(): void {
    if (!this.reportDescription.trim()) return;

    this.complaintService.submitComplaint({
      reportedUserId: this.reportUserId() || undefined,
      workspaceId: this.reportWsId() || undefined,
      messageId: this.reportMsgId() || undefined,
      category: this.reportCategory,
      description: this.reportDescription.trim()
    }).subscribe({
      next: () => {
        alert('Complaint submitted successfully to platform administrators.');
        this.showReportModal.set(false);
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to submit complaint.');
      }
    });
  }
}
