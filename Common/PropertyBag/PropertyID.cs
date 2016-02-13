namespace Shared
{
    /// <summary>
    /// Property ID values between -1000 and -1 (inclusive) are reserved
    /// </summary>
    public enum PropertyID : int
    {
        Name = -1000,
        LastLogin = -999,
        MaxPlayers = -998,
        GameId = -997,
        CharacterId = -994,
        CharacterInfo = -993,
        ServerListing = -992,
        Owner = -991,
        MaxObservers = -990,

    }
}