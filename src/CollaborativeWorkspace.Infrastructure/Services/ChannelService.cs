using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Channel;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class ChannelService : IChannelService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ChannelService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ServiceResult<IEnumerable<ChannelResponse>>> GetChannelsAsync(Guid workspaceId, Guid userId)
    {
        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive);
        if (membership is null) return ServiceResult<IEnumerable<ChannelResponse>>.Forbidden("Not a member of this workspace.");

        var channels = await _db.Channels
            .Where(c => c.WorkspaceId == workspaceId && !c.IsArchived)
            .ToListAsync();

        // Filter private channels — only include if user is in ChannelMembers or is Owner/Moderator
        var privateChannelIds = channels.Where(c => c.IsPrivate).Select(c => c.ChannelId).ToList();
        var accessiblePrivateIds = await _db.ChannelMembers
            .Where(cm => privateChannelIds.Contains(cm.ChannelId) && cm.UserId == userId)
            .Select(cm => cm.ChannelId)
            .ToListAsync();

        var isPrivileged = membership.MemberRole is "Owner" or "Moderator";
        var visible = channels.Where(c =>
            !c.IsPrivate ||
            accessiblePrivateIds.Contains(c.ChannelId) ||
            isPrivileged).ToList();

        return ServiceResult<IEnumerable<ChannelResponse>>.Success(visible.Select(MapChannel));
    }

    public async Task<ServiceResult<ChannelResponse>> CreateChannelAsync(Guid workspaceId, Guid userId, CreateChannelRequest request)
    {
        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive);

        if (membership is null || membership.MemberRole is not ("Owner" or "Moderator"))
            return ServiceResult<ChannelResponse>.Forbidden("Only owners and moderators can create channels.");

        // Premium channels require Pro/Enterprise subscription
        if (request.IsPremium)
        {
            var hasSub = await _db.Subscriptions.AnyAsync(s =>
                s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.UtcNow);
            if (!hasSub)
                return ServiceResult<ChannelResponse>.Failure("A premium subscription is required to create premium channels.", "SUBSCRIPTION_REQUIRED", 402);
        }

        var channel = new Channel
        {
            ChannelId = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ChannelName = request.ChannelName.ToLower().Trim(),
            ChannelType = request.ChannelType.ToLower(),
            IsPrivate = request.IsPrivate,
            IsPremium = request.IsPremium,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Channels.Add(channel);

        // Auto-add creator to private channel
        if (channel.IsPrivate)
            _db.ChannelMembers.Add(new ChannelMember { Id = Guid.NewGuid(), ChannelId = channel.ChannelId, UserId = userId, AddedAt = DateTime.UtcNow });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "CHANNEL_CREATE", "Channel", channel.ChannelId.ToString(), new { channel.ChannelName, workspaceId });

        return ServiceResult<ChannelResponse>.Success(MapChannel(channel), 201);
    }

    public async Task<ServiceResult<ChannelResponse>> GetChannelAsync(Guid workspaceId, Guid channelId, Guid userId)
    {
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.ChannelId == channelId && c.WorkspaceId == workspaceId);

        if (channel is null) return ServiceResult<ChannelResponse>.NotFound("Channel not found.");

        var canAccess = await CanUserAccessChannelAsync(channel, userId, workspaceId);
        if (!canAccess) return ServiceResult<ChannelResponse>.Forbidden("Access denied to this channel.");

        return ServiceResult<ChannelResponse>.Success(MapChannel(channel));
    }

    public async Task<ServiceResult<ChannelResponse>> UpdateChannelAsync(Guid workspaceId, Guid channelId, Guid userId, UpdateChannelRequest request)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId && c.WorkspaceId == workspaceId);
        if (channel is null) return ServiceResult<ChannelResponse>.NotFound("Channel not found.");

        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive);
        if (membership is null || membership.MemberRole is not ("Owner" or "Moderator"))
            return ServiceResult<ChannelResponse>.Forbidden("Only owners and moderators can update channels.");

        if (request.ChannelName is not null) channel.ChannelName = request.ChannelName.ToLower().Trim();
        if (request.IsPrivate.HasValue) channel.IsPrivate = request.IsPrivate.Value;
        if (request.IsPremium.HasValue) channel.IsPremium = request.IsPremium.Value;
        if (request.IsArchived.HasValue) channel.IsArchived = request.IsArchived.Value;
        channel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "CHANNEL_UPDATE", "Channel", channelId.ToString());
        return ServiceResult<ChannelResponse>.Success(MapChannel(channel));
    }

    public async Task<ServiceResult<string>> DeleteChannelAsync(Guid workspaceId, Guid channelId, Guid userId)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId && c.WorkspaceId == workspaceId);
        if (channel is null) return ServiceResult<string>.NotFound("Channel not found.");

        var isOwner = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.MemberRole == "Owner" && m.IsActive);
        if (!isOwner) return ServiceResult<string>.Forbidden("Only the workspace owner can delete channels.");

        channel.IsArchived = true;
        channel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "CHANNEL_DELETE", "Channel", channelId.ToString());
        return ServiceResult<string>.Success("Channel archived.");
    }

    public async Task<ServiceResult<string>> AddChannelMemberAsync(Guid workspaceId, Guid channelId, Guid requesterId, AddChannelMemberRequest request)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId && c.WorkspaceId == workspaceId);
        if (channel is null) return ServiceResult<string>.NotFound("Channel not found.");
        if (!channel.IsPrivate) return ServiceResult<string>.Failure("Cannot add members to public channels.");

        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.IsActive);
        if (membership is null || membership.MemberRole is not ("Owner" or "Moderator"))
            return ServiceResult<string>.Forbidden("Not authorized.");

        // Target must be a workspace member
        var targetMembership = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == request.UserId && m.IsActive);
        if (!targetMembership)
            return ServiceResult<string>.Failure("Target user is not a member of this workspace.");

        var alreadyInChannel = await _db.ChannelMembers.AnyAsync(cm => cm.ChannelId == channelId && cm.UserId == request.UserId);
        if (alreadyInChannel) return ServiceResult<string>.Failure("User is already in this channel.");

        _db.ChannelMembers.Add(new ChannelMember { Id = Guid.NewGuid(), ChannelId = channelId, UserId = request.UserId, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return ServiceResult<string>.Success("Member added to channel.");
    }

    public async Task<ServiceResult<string>> RemoveChannelMemberAsync(Guid workspaceId, Guid channelId, Guid targetUserId, Guid requesterId)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId && c.WorkspaceId == workspaceId);
        if (channel is null) return ServiceResult<string>.NotFound("Channel not found.");

        var membership = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.IsActive);
        if (membership is null || membership.MemberRole is not ("Owner" or "Moderator"))
            return ServiceResult<string>.Forbidden("Not authorized.");

        var cm = await _db.ChannelMembers.FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == targetUserId);
        if (cm is null) return ServiceResult<string>.NotFound("User is not in this channel.");

        _db.ChannelMembers.Remove(cm);
        await _db.SaveChangesAsync();
        return ServiceResult<string>.Success("Member removed from channel.");
    }

    private async Task<bool> CanUserAccessChannelAsync(Channel channel, Guid userId, Guid workspaceId)
    {
        var membership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive);
        if (membership is null) return false;
        if (!channel.IsPrivate) return true;
        if (membership.MemberRole is "Owner" or "Moderator") return true;
        return await _db.ChannelMembers.AnyAsync(cm => cm.ChannelId == channel.ChannelId && cm.UserId == userId);
    }

    private static ChannelResponse MapChannel(Channel c) =>
        new(c.ChannelId, c.WorkspaceId, c.ChannelName, c.ChannelType, c.IsPrivate, c.IsPremium, c.IsArchived, c.CreatedBy, c.CreatedAt);
}
