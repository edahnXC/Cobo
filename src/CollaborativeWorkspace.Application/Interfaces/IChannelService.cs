using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Channel;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IChannelService
{
    Task<ServiceResult<IEnumerable<ChannelResponse>>> GetChannelsAsync(Guid workspaceId, Guid userId);
    Task<ServiceResult<ChannelResponse>> CreateChannelAsync(Guid workspaceId, Guid userId, CreateChannelRequest request);
    Task<ServiceResult<ChannelResponse>> GetChannelAsync(Guid workspaceId, Guid channelId, Guid userId);
    Task<ServiceResult<ChannelResponse>> UpdateChannelAsync(Guid workspaceId, Guid channelId, Guid userId, UpdateChannelRequest request);
    Task<ServiceResult<string>> DeleteChannelAsync(Guid workspaceId, Guid channelId, Guid userId);
    Task<ServiceResult<string>> AddChannelMemberAsync(Guid workspaceId, Guid channelId, Guid requesterId, AddChannelMemberRequest request);
    Task<ServiceResult<string>> RemoveChannelMemberAsync(Guid workspaceId, Guid channelId, Guid targetUserId, Guid requesterId);
}
