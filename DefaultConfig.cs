using GeneralPurposeLib;

namespace FriendTuringTest; 

public static class DefaultConfig {

    public static readonly Dictionary<string, Property> Values = new() {
        { "discord_token", "xxxxxxxxxxxxx" },
        { "invite_expiry_seconds", "300" },
        { "open_ai_token", "xxxxxxxxxxxxx" },
        { "system_prompt", "" },
        { "force_ai", "False" },
        { "use_gpt4", "False" }
    };

}