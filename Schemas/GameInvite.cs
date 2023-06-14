using Discord;

namespace FriendTuringTest.Schemas; 

public class GameInvite {
    public DateTime CreatedAt { get; set; }
    public IUser Sender { get; set; } = null!;
    public IUser Recipient { get; set; } = null!;
}