export interface User {
  userId: string;
  fullName: string;
  email: string;
  isEmailVerified: boolean;
  isActive: boolean;
  avatarUrl?: string;
  roles: string[];
  currentPlan?: string;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface Workspace {
  workspaceId: string;
  name: string;
  description?: string;
  logoUrl?: string;
  isActive: boolean;
  ownerId: string;
  ownerName: string;
  memberCount: number;
  createdAt: string;
}

export interface WorkspaceMember {
  userId: string;
  fullName: string;
  email: string;
  avatarUrl?: string;
  memberRole: string;
  joinedAt: string;
}

export interface WorkspaceInvitation {
  invitationId: string;
  workspaceId: string;
  workspaceName: string;
  invitedEmail: string;
  inviterName: string;
  status: string;
  expiresAt: string;
  createdAt: string;
}

export interface Channel {
  channelId: string;
  workspaceId: string;
  channelName: string;
  channelType: string;
  isPrivate: boolean;
  isPremium: boolean;
  isArchived: boolean;
  createdBy: string;
  createdAt: string;
}

export interface Message {
  messageId: string;
  channelId: string;
  senderId: string;
  senderName: string;
  senderAvatarUrl?: string;
  content: string;
  isDeleted: boolean;
  parentMessageId?: string;
  editedAt?: string;
  sentAt: string;
  reactions: ReactionSummary[];
}

export interface ReactionSummary {
  emoji: string;
  count: number;
  reactedByMe: boolean;
}

export interface MessagePage {
  messages: Message[];
  nextCursor?: string;
  hasMore: boolean;
}

export interface Plan {
  planId: number;
  planName: string;
  price: number;
  currency: string;
  durationDays: number;
  maxMembers?: number;
  stripePriceId?: string;
  features: string[];
}

export interface Subscription {
  subscriptionId: string;
  planId: number;
  planName: string;
  status: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface Complaint {
  complaintId: string;
  reportedByUserId: string;
  reporterName: string;
  reportedUserId?: string;
  reportedUserName?: string;
  workspaceId?: string;
  messageId?: string;
  category: string;
  description: string;
  status: string;
  resolution?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  error: string;
  code?: string;
}
