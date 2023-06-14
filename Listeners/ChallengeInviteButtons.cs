using Discord.WebSocket;
using FriendTuringTest.Schemas;
using GeneralPurposeLib;
using SimpleDiscordNet.Buttons;
using SimpleDiscordNet.Commands;

namespace FriendTuringTest.Listeners; 

public class ChallengeInviteButtons {
    
    [ButtonListener("invite_accept")]
    public async Task AcceptInvite(SocketMessageComponent ctx, DiscordSocketClient client) {
        if (!GameManager.Invites.TryRemove(ctx.Message.Id, out GameInvite? invite) || invite.CreatedAt.AddMinutes(GlobalConfig.Config["invite_expiry_seconds"].Integer) < DateTime.UtcNow) {
            await ctx.RespondWithEmbedAsync("Challenge", "This invite has expired.", ResponseType.Error);
            return;
        }
        if (!GameManager.CanStartGame(ctx.Channel.Id)) {
            await ctx.RespondWithEmbedAsync("Challenge", "You cannot accept an invite while playing a game.", ResponseType.Error);
            return;
        }
        await ctx.RespondAsync(":thumbsup:");
        Logger.Debug($"Starting game in {ctx.Channel.Id}");
        await GameManager.BeginGameSequence(ctx.Channel.Id, invite);
    }
    
    [ButtonListener("invite_ignore")]
    public async Task IgnoreInvite(SocketMessageComponent ctx, DiscordSocketClient client) {
        await ctx.RespondWithEmbedAsync("Invite", "You have ignored the invite!", ResponseType.Success);
    }
    
    [ButtonListener("invite_block")]
    public async Task BlockInvite(SocketMessageComponent ctx, DiscordSocketClient client) {
        //GameInvite invite = GameManager.Invites.TryGetValue(ctx.Message.Id)
        await ctx.RespondWithEmbedAsync("Settings", "You blocked {ctx.}", ResponseType.Success);
    }

}