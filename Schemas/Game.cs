using Discord;

namespace FriendTuringTest.Schemas; 

public class Game {
    public IUser Challenger { get; set; }
    public IUser Opponent { get; set; }
    public bool IsAi { get; set; }
    public ulong ChallengerChannelId { get; set; }
    public ulong OpponentChannelId { get; set; }
    public bool IsChallengerTurn { get; set; }
    public bool IsOpponentTurn { get; set; }
    public bool ChallengerReady { get; set; }
    public bool OpponentReady { get; set; }
    //               ischallenger, message
    public List<(bool, string)> Messages { get; set; } = new();

    public Game(GameInvite invite) {
        Challenger = invite.Sender;
        Opponent = invite.Recipient;
    }
    
    public bool RandomiseAi() {
        Random random = new();
        IsAi = random.Next(0, 2) == 0;
        return IsAi;
    }
}