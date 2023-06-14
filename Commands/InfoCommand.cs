using Discord.WebSocket;
using SimpleDiscordNet.Commands;

namespace FriendTuringTest.Commands; 

public class InfoCommand {

    [SlashCommand("info", "Get info about the bot")]
    public async Task Execute(SocketSlashCommand cmd, DiscordSocketClient client) {
        await cmd.RespondWithEmbedAsync("Friend Turing Test",
            "Friend turing test allows you to see how well you're friends know you by having them talk to either you or an AI and seeing if they can tell the difference.");
    }
    
}