using BallQueue.Enums;

namespace BallQueue.Models;

/// <summary>
/// Represents a basketball game/match.
/// Tracks the two teams, officials, and game outcome.
/// </summary>
public class Game
{
    /// <summary>
    /// Unique identifier for this game.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Sequential game number within the session (1, 2, 3, ...).
    /// Immutable after game creation.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Current status of the game (Scheduled, InProgress, Finished, Cancelled).
    /// </summary>
    public GameStatus Status { get; set; } = GameStatus.Scheduled;

    /// <summary>
    /// Date and time when the game started.
    /// </summary>
    public DateTime StartDateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Date and time when the game ended.
    /// Null if game is still in progress.
    /// </summary>
    public DateTime? EndDateTime { get; set; }

    // ========== TEAM REFERENCES ==========

    /// <summary>
    /// ID of Team A.
    /// </summary>
    public Guid TeamAId { get; set; }

    /// <summary>
    /// Navigation property for Team A.
    /// </summary>
    public virtual Team? TeamA { get; set; }

    /// <summary>
    /// ID of Team B.
    /// </summary>
    public Guid TeamBId { get; set; }

    /// <summary>
    /// Navigation property for Team B.
    /// </summary>
    public virtual Team? TeamB { get; set; }

    // ========== OFFICIALS ==========

    /// <summary>
    /// ID of the player assigned as referee.
    /// Null if no referee was assigned.
    /// </summary>
    public Guid? RefereeId { get; set; }

    /// <summary>
    /// Navigation property for referee.
    /// </summary>
    public virtual Player? Referee { get; set; }

    /// <summary>
    /// ID of the player assigned as scorer.
    /// Null if no scorer was assigned.
    /// </summary>
    public Guid? ScorerId { get; set; }

    /// <summary>
    /// Navigation property for scorer.
    /// </summary>
    public virtual Player? Scorer { get; set; }

    // ========== GAME RESULT ==========

    /// <summary>
    /// Which team won the game (A, B, or null if game is not finished).
    /// Must be set when Status is changed to Finished.
    /// </summary>
    public TeamSide? Winner { get; set; }

    /// <summary>
    /// Get the losing team based on which team won.
    /// </summary>
    public TeamSide? Loser => Winner switch
    {
        TeamSide.A => TeamSide.B,
        TeamSide.B => TeamSide.A,
        null => null
    };

    // ========== SESSION ==========

    /// <summary>
    /// ID of the session this game belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property for the session.
    /// </summary>
    public virtual Session? Session { get; set; }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Gets a team by its side (A or B).
    /// </summary>
    public Team? GetTeam(TeamSide side) =>
        side == TeamSide.A ? TeamA : TeamB;

    /// <summary>
    /// Gets the winning team object if game is finished.
    /// </summary>
    public Team? GetWinningTeam() =>
        Winner switch
        {
            TeamSide.A => TeamA,
            TeamSide.B => TeamB,
            _ => null
        };

    /// <summary>
    /// Gets the losing team object if game is finished.
    /// </summary>
    public Team? GetLosingTeam() =>
        Loser switch
        {
            TeamSide.A => TeamA,
            TeamSide.B => TeamB,
            _ => null
        };

    /// <summary>
    /// Returns a summary of the game.
    /// </summary>
    public override string ToString() =>
        $"Game #{Number} ({Status}) - Status: {Status}, Winner: {Winner}";
}
