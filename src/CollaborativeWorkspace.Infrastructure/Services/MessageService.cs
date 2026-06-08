using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Message;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;
using CollaborativeWorkspace.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageService(AppDbContext db, IAuditService audit, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _audit = audit;
        _hubContext = hubContext;
    }

    public async Task<ServiceResult<MessagePageResponse>> GetMessagesAsync(Guid channelId, Guid userId, Guid? beforeCursor, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        if (!await CanAccessChannelAsync(channelId, userId))
            return ServiceResult<MessagePageResponse>.Forbidden("Access denied to this channel.");

        IQueryable<Message> query = _db.Messages
            .Where(m => m.ChannelId == channelId)
            .Include(m => m.Sender)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .OrderByDescending(m => m.SentAt);

        if (beforeCursor.HasValue)
        {
            var cursorMsg = await _db.Messages.FindAsync(beforeCursor.Value);
            if (cursorMsg is not null)
                query = query.Where(m => m.SentAt < cursorMsg.SentAt);
        }

        var messages = await query.Take(pageSize + 1).ToListAsync();
        var hasMore = messages.Count > pageSize;
        if (hasMore) messages = messages.Take(pageSize).ToList();

        var nextCursor = hasMore ? messages.Last().MessageId : (Guid?)null;

        return ServiceResult<MessagePageResponse>.Success(new MessagePageResponse(
            messages.Select(m => MapMessage(m, userId)),
            nextCursor,
            hasMore));
    }

    public async Task<ServiceResult<MessageResponse>> SendMessageAsync(Guid channelId, Guid userId, SendMessageRequest request)
    {
        if (!await CanAccessChannelAsync(channelId, userId))
            return ServiceResult<MessageResponse>.Forbidden("Access denied to this channel.");

        // Check premium channel gate
        var channel = await _db.Channels.FindAsync(channelId);
        if (channel is not null && channel.IsPremium)
        {
            var hasSub = await _db.Subscriptions.AnyAsync(s =>
                s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.UtcNow);
            if (!hasSub)
                return ServiceResult<MessageResponse>.Failure("A premium subscription is required to send messages in this channel.", "SUBSCRIPTION_REQUIRED", 402);
        }

        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            SenderId = userId,
            Content = request.Content.Trim(),
            ParentMessageId = request.ParentMessageId,
            IsDeleted = false,
            SentAt = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(userId);
        message.Sender = sender!;
        message.Reactions = new List<MessageReaction>();

        var response = MapMessage(message, userId);
        await _hubContext.Clients.Group(channelId.ToString()).SendAsync("ReceiveMessage", response);

        return ServiceResult<MessageResponse>.Success(response, 201);
    }

    public async Task<ServiceResult<MessageResponse>> EditMessageAsync(Guid channelId, Guid messageId, Guid userId, EditMessageRequest request)
    {
        var message = await _db.Messages.Include(m => m.Sender).Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ChannelId == channelId);

        if (message is null) return ServiceResult<MessageResponse>.NotFound("Message not found.");
        if (message.IsDeleted) return ServiceResult<MessageResponse>.Failure("Cannot edit a deleted message.");
        if (message.SenderId != userId) return ServiceResult<MessageResponse>.Forbidden("You can only edit your own messages.");

        message.Content = request.Content.Trim();
        message.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "MESSAGE_EDIT", "Message", messageId.ToString());
        
        var response = MapMessage(message, userId);
        await _hubContext.Clients.Group(channelId.ToString()).SendAsync("MessageEdited", response);
        
        return ServiceResult<MessageResponse>.Success(response);
    }

    public async Task<ServiceResult<string>> DeleteMessageAsync(Guid channelId, Guid messageId, Guid userId)
    {
        var message = await _db.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId && m.ChannelId == channelId);
        if (message is null) return ServiceResult<string>.NotFound("Message not found.");
        if (message.IsDeleted) return ServiceResult<string>.Failure("Message is already deleted.");

        // Sender can delete their own; Moderator/Owner of the workspace can also delete
        if (message.SenderId != userId)
        {
            var channel = await _db.Channels.FindAsync(channelId);
            var isMod = await _db.WorkspaceMembers.AnyAsync(m =>
                m.WorkspaceId == channel!.WorkspaceId && m.UserId == userId &&
                (m.MemberRole == "Owner" || m.MemberRole == "Moderator") && m.IsActive);
            if (!isMod) return ServiceResult<string>.Forbidden("You are not authorized to delete this message.");
        }

        // Soft-delete: replace content, keep row
        message.Content = "[Message deleted]";
        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "MESSAGE_DELETE", "Message", messageId.ToString());
        await _hubContext.Clients.Group(channelId.ToString()).SendAsync("MessageDeleted", messageId);
        return ServiceResult<string>.Success("Message deleted.");
    }

    public async Task<ServiceResult<string>> AddReactionAsync(Guid channelId, Guid messageId, Guid userId, AddReactionRequest request)
    {
        var message = await _db.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId && m.ChannelId == channelId);
        if (message is null || message.IsDeleted) return ServiceResult<string>.NotFound("Message not found.");

        if (!await CanAccessChannelAsync(channelId, userId))
            return ServiceResult<string>.Forbidden("Access denied.");

        var existing = await _db.MessageReactions.AnyAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == request.Emoji);
        if (existing) return ServiceResult<string>.Failure("You have already reacted with this emoji.");

        _db.MessageReactions.Add(new MessageReaction
        {
            ReactionId = Guid.NewGuid(),
            MessageId = messageId,
            UserId = userId,
            Emoji = request.Emoji,
            ReactedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await _hubContext.Clients.Group(channelId.ToString()).SendAsync("ReactionAdded", messageId, userId, request.Emoji);
        return ServiceResult<string>.Success("Reaction added.");
    }

    public async Task<ServiceResult<string>> RemoveReactionAsync(Guid channelId, Guid messageId, Guid userId, string emoji)
    {
        var reaction = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);
        if (reaction is null) return ServiceResult<string>.NotFound("Reaction not found.");

        _db.MessageReactions.Remove(reaction);
        await _db.SaveChangesAsync();
        await _hubContext.Clients.Group(channelId.ToString()).SendAsync("ReactionRemoved", messageId, userId, emoji);
        return ServiceResult<string>.Success("Reaction removed.");
    }

    private async Task<bool> CanAccessChannelAsync(Guid channelId, Guid userId)
    {
        var channel = await _db.Channels.FindAsync(channelId);
        if (channel is null) return false;

        var member = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == channel.WorkspaceId && m.UserId == userId && m.IsActive);
        if (member is null) return false;
        if (!channel.IsPrivate) return true;
        if (member.MemberRole == "Owner" || member.MemberRole == "Moderator") return true;

        return await _db.ChannelMembers.AnyAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);
    }

    private static MessageResponse MapMessage(Message m, Guid currentUserId)
    {
        var reactions = m.Reactions?
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionSummary(g.Key, g.Count(), g.Any(r => r.UserId == currentUserId)))
            .ToList() ?? [];

        return new MessageResponse(
            m.MessageId, m.ChannelId, m.SenderId,
            m.Sender?.FullName ?? string.Empty,
            m.Sender?.AvatarUrl,
            m.Content, m.IsDeleted, m.ParentMessageId,
            m.EditedAt, m.SentAt, reactions);
    }
}
