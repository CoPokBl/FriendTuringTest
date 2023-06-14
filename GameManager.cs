using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using FriendTuringTest.Schemas;
using GeneralPurposeLib;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using Game = FriendTuringTest.Schemas.Game;

namespace FriendTuringTest; 

public static class GameManager {
    //                        msg  , invite
    public static readonly ConcurrentDictionary<ulong, GameInvite> Invites = new();
    private static readonly List<Game> Games = new();
    //                                           user,  stopwatch
    private static readonly ConcurrentDictionary<ulong, Stopwatch> MessageStopwatches = new();

    private const int MaxMessages = 10;

    public static void CreateInvite(ulong msgId, IUser sender, IUser recipient) {
        Invites.TryAdd(msgId, new GameInvite {
            Sender = sender,
            Recipient = recipient,
            CreatedAt = DateTime.UtcNow
        });
    }

    public static int GetMessageWaitTime(int millisToSend, int msgLength) {
        // time to wait = (1s + 0.1s * length of message) - time taken to generate message
        int timeCalc = 1000 + 100 * msgLength - millisToSend;
        return timeCalc < 0 ? 0 : timeCalc;
    }

    public static bool IsGame(ulong channel) {
        return Games.Any(g => g.ChallengerChannelId == channel || g.OpponentChannelId == channel);
    }
    
    public static Game? GetGame(ulong channel) {
        return !IsGame(channel) ? null : Games.Single(g => g.ChallengerChannelId == channel || g.OpponentChannelId == channel);
    }
    
    public static void RemoveGame(ulong channel) {
        Games.Remove(Games.Find(g => g.ChallengerChannelId == channel || g.OpponentChannelId == channel)!);
    }

    public static async Task HandleGameMessage(SocketMessage msg) {
        Game game = GetGame(msg.Channel.Id)!;

        if (game.Messages.Count >= MaxMessages) {
            return;  // Game finished
        }

        if (game.Challenger.Id == msg.Author.Id) {
            // It's the challenger
            if (game.IsAi || !game.IsChallengerTurn) {
                IEmote cross = new Emoji("❌");
                await msg.AddReactionAsync(cross);
                return;
            }
            
            // Valid message on their turn
            
            // Get time taken to send message
            MessageStopwatches[msg.Author.Id].Stop();
            TimeSpan timeTaken = MessageStopwatches[msg.Author.Id].Elapsed;
            MessageStopwatches.Remove(msg.Author.Id, out _);
            
            // Get the time we need to wait
            int timeToWait = GetMessageWaitTime((int) timeTaken.TotalMilliseconds, msg.Content.Length);
            
            IEmote tick = new Emoji("✅");
            await msg.AddReactionAsync(tick);
            
            game.IsChallengerTurn = false;
            game.IsOpponentTurn = false;
            Games.Remove(Games.Find(g => g.ChallengerChannelId == game.ChallengerChannelId)!);
            Games.Add(game);
            
            Thread.Sleep(timeToWait);
            await game.Opponent.SendMessageAsync($"{game.Challenger.Mention}: {msg.Content}");
            game.IsOpponentTurn = true;
            game.Messages.Add((true, msg.Content));
            Games.Remove(Games.Find(g => g.ChallengerChannelId == game.ChallengerChannelId)!);
            Games.Add(game);
            await CheckGameFinish(game);
        }
        else if (game.Opponent.Id == msg.Author.Id) {
            // It's the opponent
            if (!game.IsOpponentTurn) {
                IEmote cross = new Emoji("❌");
                await msg.AddReactionAsync(cross);
                return;
            }
            
            // Valid message on their turn
            await game.Challenger.SendMessageAsync($"{game.Opponent.Mention}: {msg.Content}");
            
            // Start stopwatch
            MessageStopwatches.TryAdd(msg.Author.Id, new Stopwatch());
            MessageStopwatches[msg.Author.Id].Start();

            if (!game.IsAi) {
                game.IsChallengerTurn = true;
                game.Messages.Add((false, msg.Content));
                IEmote tick = new Emoji("✅");
                await msg.AddReactionAsync(tick);
                RemoveGame(game.ChallengerChannelId);
                Games.Add(game);
                await CheckGameFinish(game);
                return;
            }
            game.Messages.Add((false, msg.Content));
            
            // AI game
            Model model = Model.GPT4;
            if (!GlobalConfig.Config["use_gpt4"]) {
                model = Model.GPT3_5_Turbo;
            }
            OpenAIClient client = new(GlobalConfig.Config["open_ai_token"].Text);
            List<Message> chatMessages = new() {new Message(Role.System, GlobalConfig.Config["system_prompt"])};
            chatMessages.AddRange(game.Messages.Select(message => new Message(message.Item1 ? Role.Assistant : Role.User, message.Item2)));
            ChatRequest request = new(chatMessages, model, maxTokens: 128, user: msg.Author.Id.ToString());
            
            Stopwatch stopwatch = new();
            stopwatch.Start();
            ChatResponse? response = await client.ChatEndpoint.GetCompletionAsync(request);
            string aiMsg = response.FirstChoice.Message;
            
            stopwatch.Stop();
            
            // get time to wait = (1s + 0.1s * length of message) - time taken to generate message
            int timeToWait = GetMessageWaitTime((int) stopwatch.ElapsedMilliseconds, aiMsg.Length);

            IEmote tick2 = new Emoji("✅");
            await msg.AddReactionAsync(tick2);
            
            game.IsOpponentTurn = false;
            Games.Remove(Games.Find(g => g.ChallengerChannelId == game.ChallengerChannelId)!);
            Games.Add(game);

            Thread.Sleep(timeToWait);
            await game.Challenger.SendMessageAsync($"AI (Pretending to be you): {aiMsg}");
            await game.Opponent.SendMessageAsync($"{game.Challenger.Mention}: {aiMsg}");
                
            game.Messages.Add((true, aiMsg));
            game.IsOpponentTurn = true;
            RemoveGame(game.ChallengerChannelId);
            Games.Add(game);
            await CheckGameFinish(game);
        }
    }

    public static async Task CheckGameFinish(Game game) {
        if (game.Messages.Count < MaxMessages) {
            return;
        }
        
        // Set both turns to false
        game.IsChallengerTurn = false;
        game.IsOpponentTurn = false;
        RemoveGame(game.ChallengerChannelId);
        Games.Add(game);
        
        Embed gameInfoEmbed = new EmbedBuilder()
            .WithTitle("Game")
            .WithDescription($"**The game is now over.**" +
                             $"{game.Opponent.Mention} will now try and guess whether or not they talked to you or an AI chatbot.\n" +
                             $"Please wait while they make their decision.")
            .WithColor(Color.Green)
            .Build();
        await game.Challenger.SendMessageAsync(embed: gameInfoEmbed);
        
        gameInfoEmbed = new EmbedBuilder()
            .WithTitle("Game")
            .WithDescription($"**The game is now over.**" +
                             $"You must now decide, did you just have a conversation with {game.Challenger.Mention} or an AI chatbot?")
            .WithColor(Color.Green)
            .Build();
        
        ButtonBuilder humanButton = new ButtonBuilder()
            .WithLabel("Human")
            .WithStyle(ButtonStyle.Success)
            .WithCustomId("decide_human");
        ButtonBuilder aiButton = new ButtonBuilder()
            .WithLabel("AI")
            .WithStyle(ButtonStyle.Danger)
            .WithCustomId("decide_ai");
        MessageComponent component = new ComponentBuilder()
            .WithButton(humanButton)
            .WithButton(aiButton)
            .Build();
        
        await game.Opponent.SendMessageAsync(embed: gameInfoEmbed, components: component);
    }
    
    public static bool CanStartGame(ulong channel) {
        return !IsGame(channel);
    }
    
    public static bool HasUserAlreadyChallenged(ulong sender, ulong recipient) {
        return Invites.Values.Any(invite => invite.Sender.Id == sender && invite.Recipient.Id == recipient);
    }

    public static async Task BeginGameSequence(ulong channel, GameInvite invite) {
        Game game = new(invite);
        game.RandomiseAi();
        if (GlobalConfig.Config["force_ai"]) {
            game.IsAi = true;
        }
        game.OpponentChannelId = channel;

        // Challenger messages
        Embed challengeAcceptedEmbed = new EmbedBuilder()
            .WithTitle("Challenge")
            .WithDescription($"{game.Opponent.Mention} has accepted your challenge!")
            .WithColor(Color.Green)
            .Build();
        Embed gameInfoEmbed;
        if (game.IsAi) {
            gameInfoEmbed = new EmbedBuilder()
                .WithTitle("Game")
                .WithDescription($"**{game.Opponent.Mention} will begin chatting with an AI chatbot pretending to be you.**\n" +
                                 $"Once your opponent has finished talking with the AI bot they will have to guess whether it was you or the AI they were talking to.\n" +
                                 $"Please do not talk to your opponent until they have finished talking to the AI bot. You will be talk when they finish talking.\n" +
                                 $"You will receive the conversion that your opponent is having as it happens.\n\n" +
                                 $"Click the button below to begin the game. **THE GAME WILL GO FOR 10 MESSAGES IN TOTAL**")
                .WithColor(Color.Green)
                .Build();
        }
        else {
            gameInfoEmbed = new EmbedBuilder()
                .WithTitle("Game")
                .WithDescription($"**{game.Opponent.Mention} will begin chatting with you** through this DM channel.\n" +
                                 $"Once your opponent has finished talking with you they will have to guess whether it was you or an AI chatbot they were talking to.\n" +
                                 $"Your goal is to make them think that you are an AI chatbot. This means that you should not say anything that would tell them that it's you.\n" +
                                 $"Do not DM them during the game. Once they send a message you will see it appear here, and you will be able to respond to it.\n\n" +
                                 $"Click the button below to begin the game. **THE GAME WILL GO FOR 10 MESSAGES IN TOTAL**")
                .WithColor(Color.Green)
                .Build();
        }
        ButtonBuilder startButton = new ButtonBuilder()
            .WithLabel("I'm Ready")
            .WithStyle(ButtonStyle.Success)
            .WithCustomId("game_ready");
        MessageComponent component = new ComponentBuilder()
            .WithButton(startButton)
            .Build();
        IUserMessage sentMessage = await game.Challenger.SendMessageAsync(embeds: new [] { challengeAcceptedEmbed, gameInfoEmbed }, components: component);
        game.ChallengerChannelId = sentMessage.Channel.Id;
        
        // Opponent messages
        gameInfoEmbed = new EmbedBuilder()
            .WithTitle("Game")
            .WithDescription($"**Welcome to the Friend Turing Test**\n" +
                             $"How well do you know {game.Challenger.Mention}? After the game starts you will begin talking in this channel.\n" +
                             $"You will have to guess whether you are talking to {game.Challenger.Mention} or an AI chatbot pretending to be them.\n" +
                             $"Do not DM {game.Challenger.Mention} during the game. Once you send a message you will see it appear here, and they will be able to respond to it.\n\n" +
                             $"Click the button below to begin the game. **THE GAME WILL GO FOR 10 MESSAGES IN TOTAL**")
            .WithColor(Color.Green)
            .Build();
        await game.Opponent.SendMessageAsync(embeds: new [] { gameInfoEmbed }, components: component);

        Games.Add(game);
    }

    public static async Task StartGame(Game game) {
        Embed embed = new EmbedBuilder()
            .WithTitle("Game")
            .WithDescription($"**The game has started**\n" +
                             $"You will take turns sending messages. You will go first.\n" +
                             $"You may now start sending messages, once you have sent a message please wait for your opponent (or the AI chatbot) to send a message.")
            .WithColor(Color.Green)
            .Build();
        await game.Opponent.SendMessageAsync(embed: embed);

        if (game.IsAi) {
            embed = new EmbedBuilder()
                .WithTitle("Game")
                .WithDescription($"**The game has started**\n" +
                                 $"{game.Opponent.Mention} and the AI chatbot will take turns sending messages.\n" +
                                 $"{game.Opponent.Mention} will go first. You will see all the messages.")
                .WithColor(Color.Green)
                .Build();
        }
        else {
            embed = new EmbedBuilder()
                .WithTitle("Game")
                .WithDescription($"**The game has started**\n" +
                                 $"You will take turns sending messages. Your opponent will go first.\n" +
                                 $"You may now start sending messages, once your opponent has sent a message please wait for them to send another message.\n" +
                                 $"Try to make your opponent think that you are an AI chatbot.")
                .WithColor(Color.Green)
                .Build();
        }
        await game.Challenger.SendMessageAsync(embed: embed);
    }

}