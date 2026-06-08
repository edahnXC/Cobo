namespace CollaborativeWorkspace.Application.DTOs.Message;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record SendMessageRequest(
    string Content,
    Guid? ParentMessageId
);

public record EditMessageRequest(
    string Content
);

public record AddReactionRequest(
    string Emoji
);

public record GetMessagesRequest
{
    public Guid? BeforeCursor { get; init; }  // For cursor-based pagination
    public int PageSize { get; init; } = 50;
}

// ─── Responses ────────────────────────────────────────────────────────────────

public record MessageResponse(
    Guid MessageId,
    Guid ChannelId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    string Content,
    bool IsDeleted,
    Guid? ParentMessageId,
    DateTime? EditedAt,
    DateTime SentAt,
    IEnumerable<ReactionSummary> Reactions
);

public record ReactionSummary(
    string Emoji,
    int Count,
    bool CurrentUserReacted
);

public record MessagePageResponse(
    IEnumerable<MessageResponse> Messages,
    Guid? NextCursor,
    bool HasMore
);
