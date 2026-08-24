namespace BallQueue.Enums;

/// <summary>
/// Represents the possible statuses of a game.
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Game has been created but not yet started.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Game is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Game has finished with a winner determined.
    /// </summary>
    Finished = 2,

    /// <summary>
    /// Game was cancelled before completion.
    /// </summary>
    Cancelled = 3
}
