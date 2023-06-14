using Discord;
using Discord.WebSocket;
using GeneralPurposeLib;
using SimpleDiscordNet.Buttons;
using SimpleDiscordNet.Commands;
using Game = FriendTuringTest.Schemas.Game;

namespace FriendTuringTest.Listeners; 

public class GameButtonListeners {
    
    [ButtonListener("game_ready")]
    public async Task GameReady(SocketMessageComponent ctx, DiscordSocketClient client) {
        if (!GameManager.IsGame(ctx.Channel.Id)) {
            await ctx.RespondWithEmbedAsync("Game", "This is not a game channel.", ResponseType.Error);
            Logger.Debug("Not a game channel: " + ctx.Channel.Id);
            return;
        }
        Game game = GameManager.GetGame(ctx.Channel.Id)!;
        if (game.Challenger.Id != ctx.User.Id && game.Opponent.Id != ctx.User.Id) {
            await ctx.RespondWithEmbedAsync("Game", "You are not in this game.", ResponseType.Error);
            return;
        }
        if (game.Challenger.Id == ctx.User.Id) {
            game.ChallengerReady = true;
        } else {
            game.OpponentReady = true;
        }
        if (game is {ChallengerReady: true, OpponentReady: true}) {
            await ctx.RespondWithEmbedAsync("Game", "Both players are ready! The game will begin shortly.", ResponseType.Success);
            await GameManager.StartGame(game);
            return;
        }
        await ctx.RespondWithEmbedAsync("Game", "You are ready! Once your opponent readies the game will begin.", ResponseType.Success);
    }

    [ButtonListener("decide_human")]
    public async Task DecideHuman(SocketMessageComponent ctx, DiscordSocketClient client) {
        Game? game = GameManager.GetGame(ctx.Channel.Id);
        if (game == null) {
            await ctx.RespondWithEmbedAsync("Game", "This is not a game channel.", ResponseType.Error);
            return;
        }
        
        if (game.Challenger.Id != ctx.User.Id && game.Opponent.Id != ctx.User.Id) {
            await ctx.RespondWithEmbedAsync("Game", "You are not in this game.", ResponseType.Error);
            return;
        }

        if (game.IsAi) {
            // Wrong
            await ctx.RespondWithEmbedAsync("Result", $"You were wrong! The other player was an AI chatbot. It turns out you don't know {game.Challenger.Mention} very well", ResponseType.Error);
            Embed theyGotItWrong = new EmbedBuilder()
                .WithTitle("Result")
                .WithDescription($"{game.Opponent.Mention} got it wrong. They clearly don't know you very well.")
                .WithColor(Color.Red)
                .Build();
            await game.Challenger.SendMessageAsync(embed: theyGotItWrong);
            GameManager.RemoveGame(game.ChallengerChannelId);
            return;
        }
        // Right
        await ctx.RespondWithEmbedAsync("Result", $"You were right! The other player was a human. You know {game.Challenger.Mention} very well", ResponseType.Success);
        Embed theyGotItRight = new EmbedBuilder()
            .WithTitle("Result")
            .WithDescription($"{game.Opponent.Mention} got it right. They were not fooled, they know you quite well.")
            .WithColor(Color.Green)
            .Build();
        await game.Challenger.SendMessageAsync(embed: theyGotItRight);
        GameManager.RemoveGame(game.ChallengerChannelId);
    }
    
    [ButtonListener("decide_ai")]
    public async Task DecideAi(SocketMessageComponent ctx, DiscordSocketClient client) {
        Game? game = GameManager.GetGame(ctx.Channel.Id);
        if (game == null) {
            await ctx.RespondWithEmbedAsync("Game", "This is not a game channel.", ResponseType.Error);
            return;
        }
        
        if (game.Challenger.Id != ctx.User.Id && game.Opponent.Id != ctx.User.Id) {
            await ctx.RespondWithEmbedAsync("Game", "You are not in this game.", ResponseType.Error);
            return;
        }

        if (!game.IsAi) {
            // Wrong
            await ctx.RespondWithEmbedAsync("Result", $"You were wrong! The other player was a human. It turns out you don't know {game.Challenger.Mention} very well", ResponseType.Error);
            Embed theyGotItWrong = new EmbedBuilder()
                .WithTitle("Result")
                .WithDescription($"{game.Opponent.Mention} got it wrong. They clearly don't know you very well.")
                .WithColor(Color.Red)
                .Build();
            await game.Challenger.SendMessageAsync(embed: theyGotItWrong);
            GameManager.RemoveGame(game.ChallengerChannelId);
            return;
        }
        // Right
        await ctx.RespondWithEmbedAsync("Result", $"You were right! The other player was an AI chatbot. You know {game.Challenger.Mention} very well", ResponseType.Success);
        Embed theyGotItRight = new EmbedBuilder()
            .WithTitle("Result")
            .WithDescription($"{game.Opponent.Mention} got it right. They were not fooled, they know you quite well.")
            .WithColor(Color.Green)
            .Build();
        await game.Challenger.SendMessageAsync(embed: theyGotItRight);
        GameManager.RemoveGame(game.ChallengerChannelId);
    }
    
}