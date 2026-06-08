using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Message;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IMessageService
{
    Task<ServiceResult<MessagePageResponse>> GetMessagesAsync(Guid channelId, Guid userId, Guid? beforeCursor, int pageSize);
    Task<ServiceResult<MessageResponse>> SendMessageAsync(Guid channelId, Guid userId, SendMessageRequest request);
    Task<ServiceResult<MessageResponse>> EditMessageAsync(Guid channelId, Guid messageId, Guid userId, EditMessageRequest request);
    Task<ServiceResult<string>> DeleteMessageAsync(Guid channelId, Guid messageId, Guid userId);
    Task<ServiceResult<string>> AddReactionAsync(Guid channelId, Guid messageId, Guid userId, AddReactionRequest request);
    Task<ServiceResult<string>> RemoveReactionAsync(Guid channelId, Guid messageId, Guid userId, string emoji);
}
