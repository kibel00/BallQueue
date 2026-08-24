using BallQueue.Enums;

namespace BallQueue.Models;

/// <summary>
/// Represents a basketball player in the queue management system.
/// Each player has an immutable arrival number and tracks their participation history.
/// </summary>
public class Player
{
    /// <summary>
    /// Unique identifier for the player.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Player's name used for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Immutable sequential number assigned at registration (1, 2, 3, ...).
    /// This never changes and serves as the primary ordering mechanism.
    /// </summary>
    public int ArrivalNumber { get; set; }

    /// <summary>
    /// Exact date and time when the player registered.
    /// Used for tie-breaking and historical tracking.
    /// </summary>
    public DateTime ArrivalDateTime { get; set; }

    /// <summary>
    /// Current status of the player in the game queue.
    /// Determines whether player is waiting, playing, refereeing, etc.
    /// </summary>
    public PlayerStatus CurrentStatus { get; set; } = PlayerStatus.Waiting;

    // ========== PAYMENT INFORMATION ==========

    /// <summary>
    /// Whether the player has paid the required fee (RD$100).
    /// Affects queue priority if PaymentPriorityEnabled is true.
    /// </summary>
    public bool HasPaid { get; set; } = false;

    /// <summary>
    /// Total amount paid by this player.
    /// Allows tracking of partial or multiple payments.
    /// </summary>
    public decimal AmountPaid { get; set; } = 0;

    /// <summary>
    /// Date and time of the most recent payment.
    /// </summary>
    public DateTime? PaymentDateTime { get; set; }

    // ========== GAME STATISTICS ==========

    /// <summary>
    /// Total number of games this player has played (not including referee/scorer duties).
    /// Incremented when player is in a playing team.
    /// </summary>
    public int GamesPlayed { get; set; } = 0;

    /// <summary>
    /// Total number of games this player has waited through (not playing or officiating).
    /// Incremented each game while in waiting status.
    /// </summary>
    public int GamesWaiting { get; set; } = 0;

    /// <summary>
    /// Number of consecutive games this player has played without losing.
    /// Resets to 0 when player's team loses a game.
    /// Used to implement MaxConsecutiveGames rule.
    /// </summary>
    public int ConsecutiveGames { get; set; } = 0;

    /// <summary>
    /// The game number of the last time this player participated (playing, refereeing, or scoring).
    /// Used to track inactivity duration.
    /// </summary>
    public int? LastGameNumber { get; set; }

    /// <summary>
    /// The exact date and time of the last participation.
    /// Complementary to LastGameNumber.
    /// </summary>
    public DateTime? LastPlayedDateTime { get; set; }

    // ========== SESSION & FOREIGN KEYS ==========

    /// <summary>
    /// ID of the basketball session this player belongs to.
    /// Allows separation of multiple sessions/days.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property for the session.
    /// </summary>
    public virtual Session? Session { get; set; }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Determines if this player is currently available to play (not playing/refereeing).
    /// </summary>
    public bool IsAvailable =>
        CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting;

    /// <summary>
    /// Determines if player is in an active role (playing, refereeing, or scoring).
    /// </summary>
    public bool IsActive =>
        CurrentStatus is PlayerStatus.Playing or PlayerStatus.Referee or PlayerStatus.Scorer;

    /// <summary>
    /// Returns a short summary for debugging and logging.
    /// </summary>
    public override string ToString() =>
        $"Player #{ArrivalNumber}: {Name} (Status: {CurrentStatus}, Paid: {HasPaid}, Games: {GamesPlayed})";
}
