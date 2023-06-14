using Discord;
using FriendTuringTest;
using GeneralPurposeLib;
using SimpleDiscordNet;

Logger.Init(LogLevel.Debug);

GlobalConfig.Init(new Config(DefaultConfig.Values));

SimpleDiscordBot bot = new(GlobalConfig.Config["discord_token"]);
bot.Log += log => {
    LogLevel level = log.Severity switch {
        LogSeverity.Critical => LogLevel.Error,
        LogSeverity.Debug => LogLevel.Debug,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Info => LogLevel.Info,
        LogSeverity.Verbose => LogLevel.Debug,
        LogSeverity.Warning => LogLevel.Warn,
        _ => LogLevel.Info
    };
    if (log.Message == null) {
        Logger.Log(log.Exception, level);
        return Task.CompletedTask;
    }
    Logger.Log(log.Message, level);
    return Task.CompletedTask;
};

await bot.StartBot();

bot.Client.Ready += () => {
    if (args.Length > 0) {
        switch (args[0]) {
            case "updatecmds":
                Logger.Info("Updating Commands");
                bot.UpdateCommands();
                break;
        }
    }
    return Task.CompletedTask;
};

bot.Wait();
Logger.WaitFlush();
return 0;