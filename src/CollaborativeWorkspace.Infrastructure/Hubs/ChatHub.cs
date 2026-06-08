using Microsoft.AspNetCore.SignalR;

namespace CollaborativeWorkspace.Infrastructure.Hubs;

public class ChatHub : Hub
{
    public async Task JoinChannel(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
    }

    public async Task LeaveChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
    }

    public async Task SendGameEvent(string channelId, string eventType, string payload)
    {
        await Clients.Group(channelId).SendAsync("GameEventReceived", eventType, payload);
    }
}
