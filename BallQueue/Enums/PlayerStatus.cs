namespace BallQueue.Enums;

/// <summary>
/// Represents the various states a player can have in the queue system.
/// </summary>
public enum PlayerStatus
{
    /// <summary>
    /// Player is waiting for their turn to play.
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// Player is currently playing in a game (part of one of the two teams).
    /// </summary>
    Playing = 1,

    /// <summary>
    /// Player is assigned as a referee for the current game.
    /// </summary>
    Referee = 2,

    /// <summary>
    /// Player is assigned as a scorer/scorekeeper for the current game.
    /// </summary>
    Scorer = 3,

    /// <summary>
    /// Player's team just lost and they are in the waiting queue (possibly in limbo status).
    /// This is different from Waiting because it distinguishes recent losers for rule processing.
    /// </summary>
    LostWaiting = 4,

    /// <summary>
    /// Player has finished their session (no longer participating).
    /// </summary>
    Finished = 5,

    /// <summary>
    /// Player has been removed from the system.
    /// </summary>
    Removed = 6
}
