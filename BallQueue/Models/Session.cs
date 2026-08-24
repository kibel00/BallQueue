namespace BallQueue.Models;

/// <summary>
/// Represents a basketball session (e.g., a single day of play).
/// Groups players, games, and payments for a specific time period.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date and time when the session started.
    /// </summary>
    public DateTime StartDateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Date and time when the session ended.
    /// Null if session is still in progress.
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    /// <summary>
    /// Optional name or location of the session (e.g., "Friday Morning - Gym A").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Total number of games played in this session.
    /// Useful for quick reference without counting games collection.
    /// </summary>
    public int TotalGamesPlayed { get; set; } = 0;

    // ========== NAVIGATION PROPERTIES ==========

    /// <summary>
    /// Collection of all players registered in this session.
    /// </summary>
    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    /// <summary>
    /// Collection of all games played in this session.
    /// </summary>
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();

    /// <summary>
    /// Collection of all payments made in this session.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Whether the session is currently active (not ended).
    /// </summary>
    public bool IsActive => EndDateTime == null;

    /// <summary>
    /// Gets the duration of the session in minutes.
    /// </summary>
    public int GetDurationMinutes()
    {
        var endTime = EndDateTime ?? DateTime.Now;
        return (int)(endTime - StartDateTime).TotalMinutes;
    }

    /// <summary>
    /// Returns a summary of the session.
    /// </summary>
    public override string ToString() =>
        $"Session {Name} - Started: {StartDateTime:g}, Games: {TotalGamesPlayed}";
}
