namespace CollaborativeWorkspace.Application.DTOs.Channel;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record CreateChannelRequest(
    string ChannelName,
    string ChannelType,   // text | voice | game
    bool IsPrivate,
    bool IsPremium
);

public record UpdateChannelRequest(
    string? ChannelName,
    bool? IsPrivate,
    bool? IsPremium,
    bool? IsArchived
);

public record AddChannelMemberRequest(
    Guid UserId
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record ChannelResponse(
    Guid ChannelId,
    Guid WorkspaceId,
    string ChannelName,
    string ChannelType,
    bool IsPrivate,
    bool IsPremium,
    bool IsArchived,
    Guid CreatedBy,
    DateTime CreatedAt
);
