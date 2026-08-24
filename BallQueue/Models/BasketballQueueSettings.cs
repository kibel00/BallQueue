namespace BallQueue.Models;

/// <summary>
/// Configuration settings for the basketball queue management system.
/// All settings are configurable to allow different rule variations.
/// </summary>
public class BasketballQueueSettings
{
    // ========== GAME SETUP ==========

    /// <summary>
    /// Number of players per team (default: 5).
    /// Standard basketball is 5v5, but this can be adjusted for different formats.
    /// </summary>
    public int PlayersPerTeam { get; set; } = 5;

    /// <summary>
    /// Number of referees from the waiting queue (default: 1).
    /// </summary>
    public int RefereeCount { get; set; } = 1;

    /// <summary>
    /// Number of scorers/scorekeepers from the waiting queue (default: 1).
    /// </summary>
    public int ScorerCount { get; set; } = 1;

    // ========== PAYMENT CONFIGURATION ==========

    /// <summary>
    /// Required fee for each player to participate (default: 100 RD$).
    /// </summary>
    public decimal PlayerFee { get; set; } = 100;

    /// <summary>
    /// Whether to enable payment-based priority (default: true).
    /// If true, players who have paid get priority over those who haven't paid.
    /// Within each payment group, arrival order is still respected.
    /// </summary>
    public bool PaymentPriorityEnabled { get; set; } = true;

    /// <summary>
    /// Whether to allow partial payments (default: false).
    /// If false, player must pay full PlayerFee to be marked as "paid".
    /// If true, any payment moves player into paid category, but tracking amount is still recorded.
    /// </summary>
    public bool AllowPartialPayment { get; set; } = false;

    // ========== QUEUE BEHAVIOR ==========

    /// <summary>
    /// Whether to strictly respect arrival order within each payment group (default: true).
    /// If true, players who arrive earlier always get priority over later arrivals in same group.
    /// Should generally be true for fairness.
    /// </summary>
    public bool RespectArrivalOrder { get; set; } = true;

    /// <summary>
    /// Whether to track the number of games each player has been waiting (default: true).
    /// If true, players waiting longer may have slight priority in edge cases, but still secondary to arrival.
    /// </summary>
    public bool TrackWaitingGames { get; set; } = true;

    // ========== CONSECUTIVE GAMES PREVENTION ==========

    /// <summary>
    /// Whether to implement protection against excessive consecutive games (default: true).
    /// If true, a player who has already played MaxConsecutiveGames in a row
    /// will be deprioritized in favor of players who haven't reached the limit.
    /// </summary>
    public bool PreventConsecutiveGames { get; set; } = true;

    /// <summary>
    /// Maximum consecutive games a player can play before being deprioritized (default: 2).
    /// Only applies if PreventConsecutiveGames is true.
    /// Example: If set to 2, a player can play game 1 and 2, but is deprioritized for game 3
    /// unless there aren't enough other players.
    /// </summary>
    public int MaxConsecutiveGames { get; set; } = 2;

    // ========== FAIRNESS & SPECIAL RULES ==========

    /// <summary>
    /// Whether newly arrived players are automatically deferred until session evening (default: false).
    /// If true, provides a grace period before new players participate.
    /// </summary>
    public bool DeferNewPlayersInitially { get; set; } = false;

    /// <summary>
    /// Minimum number of players required to start a game (default: 10).
    /// Referees and scorers are assigned when enough additional players are available.
    /// </summary>
    public int MinPlayersToStartGame => PlayersPerTeam * 2;

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Validates that settings are internally consistent.
    /// Throws if configuration is invalid.
    /// </summary>
    public void Validate()
    {
        if (PlayersPerTeam < 1)
            throw new ArgumentException("PlayersPerTeam must be at least 1");

        if (PlayerFee < 0)
            throw new ArgumentException("PlayerFee cannot be negative");

        if (MaxConsecutiveGames < 1)
            throw new ArgumentException("MaxConsecutiveGames must be at least 1");

        if (RefereeCount < 0)
            throw new ArgumentException("RefereeCount cannot be negative");

        if (ScorerCount < 0)
            throw new ArgumentException("ScorerCount cannot be negative");
    }

    /// <summary>
    /// Returns a summary of the most important settings.
    /// </summary>
    public override string ToString() =>
        $"Basketball Settings: {PlayersPerTeam}v{PlayersPerTeam}, " +
        $"Fee: RD${PlayerFee}, " +
        $"PaymentPriority: {PaymentPriorityEnabled}, " +
        $"MaxConsecutiveGames: {MaxConsecutiveGames}";
}
