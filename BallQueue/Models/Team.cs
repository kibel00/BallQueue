using BallQueue.Enums;

namespace BallQueue.Models;

/// <summary>
/// Represents a team in a basketball game.
/// Contains the list of players on the team and identifies which side of the game they play.
/// </summary>
public class Team
{
    /// <summary>
    /// Unique identifier for this team.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Which side of the game this team plays on (Side A or Side B).
    /// </summary>
    public TeamSide Side { get; set; }

    /// <summary>
    /// ID of the game this team participates in.
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Navigation property for the game.
    /// </summary>
    public virtual Game? Game { get; set; }

    /// <summary>
    /// Collection of Player IDs on this team.
    /// Stored as comma-separated string in SQLite for simplicity.
    /// </summary>
    public string PlayerIdsJson { get; set; } = "[]";

    /// <summary>
    /// Gets the player list from the JSON representation.
    /// Note: In practice, should load from database with proper navigation.
    /// </summary>
    public List<Guid> GetPlayerIds()
    {
        if (string.IsNullOrEmpty(PlayerIdsJson) || PlayerIdsJson == "[]")
            return new List<Guid>();

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(PlayerIdsJson);
            var ids = new List<Guid>();
            foreach (var element in json.RootElement.EnumerateArray())
            {
                if (Guid.TryParse(element.GetString(), out var id))
                    ids.Add(id);
            }
            return ids;
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Sets the player list as JSON.
    /// </summary>
    public void SetPlayerIds(List<Guid> playerIds)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(playerIds);
        PlayerIdsJson = json;
    }

    /// <summary>
    /// Common team count validation.
    /// </summary>
    public bool IsValidTeamSize(int expectedSize) =>
        GetPlayerIds().Count == expectedSize;

    /// <summary>
    /// Returns a summary of the team.
    /// </summary>
    public override string ToString() =>
        $"Team {Side} - Players: {GetPlayerIds().Count}";
}
