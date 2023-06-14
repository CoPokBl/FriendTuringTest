using Discord.WebSocket;
using SimpleDiscordNet.DMs;

namespace FriendTuringTest.Listeners; 

public class MessageListener {
    
    [DmListener]
    public Task OnDm(SocketMessage msg, DiscordSocketClient client) {
        if (!GameManager.IsGame(msg.Channel.Id)) {
            return Task.CompletedTask;
        }
#pragma warning disable CS4014 // I don't want to await it, I want it to run in the background and not block the main thread.
        GameManager.HandleGameMessage(msg);
        return Task.CompletedTask;
#pragma warning restore CS4014
    }
    
}