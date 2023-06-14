using Discord;
using Discord.WebSocket;
using SimpleDiscordNet.Commands;

namespace FriendTuringTest.Commands; 

public class ChallengeCommand {
    
    [SlashCommand("challenge", "Challenge a friend to a turing test")]
    [SlashCommandArgument("user", "The user to challenge", true, ApplicationCommandOptionType.User)]
    public async Task Execute(SocketSlashCommand cmd, DiscordSocketClient client) {
        IUser user = cmd.GetArgument<SocketUser>("user")!;
        if (user.Id == cmd.User.Id) {
            await cmd.RespondWithEmbedAsync("Challenge", "You can't challenge yourself!", ResponseType.Error);
            return;
        }

        if (GameManager.HasUserAlreadyChallenged(cmd.User.Id, user.Id)) {
            await cmd.RespondWithEmbedAsync("Challenge", "You have already challenged this user!", ResponseType.Error);
            return;
        }
        
        Embed notificationEmbed = new EmbedBuilder()
            .WithTitle("Challenge")
            .WithDescription($"{cmd.User.Mention} has challenged you to a turing test!")
            .WithColor(Color.Green)
            .Build();
        ButtonBuilder acceptButton = new ButtonBuilder()
            .WithLabel("Accept")
            .WithStyle(ButtonStyle.Success)
            .WithCustomId("invite_accept");
        ButtonBuilder ignoreButton = new ButtonBuilder()
            .WithLabel("Ignore")
            .WithStyle(ButtonStyle.Secondary)
            .WithCustomId("invite_ignore");
        ButtonBuilder blockButton = new ButtonBuilder()
            .WithLabel("Block")
            .WithStyle(ButtonStyle.Danger)
            .WithCustomId("invite_block");
        MessageComponent component = new ComponentBuilder()
            .WithButton(acceptButton)
            .WithButton(ignoreButton)
            .WithButton(blockButton)
            .Build();

        IUserMessage inviteMsg;
        try {
            inviteMsg = await user.SendMessageAsync(embed: notificationEmbed, components: component);
        }
        catch (Exception) {
            // Can't send message to user
            await cmd.RespondWithEmbedAsync("Challenge", "This user cannot be challenged.", ResponseType.Error);
            return;
        }

        await cmd.RespondWithEmbedAsync("Challenge", $"You have challenged {user.Mention} to a turing test!");
        GameManager.CreateInvite(inviteMsg.Id, cmd.User, user);
    }
    
}