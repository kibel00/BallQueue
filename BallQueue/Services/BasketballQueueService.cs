using BallQueue.Enums;
using BallQueue.Models;
using System.Collections.Generic;
using System.Linq;

namespace BallQueue.Services;

/// <summary>
/// Core service for managing the basketball queue system.
/// Implements the rotation, priority, and game creation algorithms.
/// This is the business logic engine for the application.
/// </summary>
public class BasketballQueueService
{
    private readonly BasketballQueueSettings _settings;
    private readonly IEnumerable<Player>? _players;

    /// <summary>
    /// Creates a new instance of the basketball queue service.
    /// </summary>
    /// <param name="settings">Configuration settings for the queue system.</param>
    public BasketballQueueService(BasketballQueueSettings? settings = null)
    {
        _settings = settings ?? new BasketballQueueSettings();
        _settings.Validate();
    }

    // ========== PRIORITY CALCULATION ==========

    /// <summary>
    /// Builds the effective queue order based on priority rules.
    /// This is the PRIMARY algorithm that respects payment priority and arrival order.
    /// </summary>
    /// <param name="players">All players to sort (typically from a session).</param>
    /// <returns>Sorted list of players in queue priority order.</returns>
    public List<Player> BuildEffectiveQueue(IEnumerable<Player> players)
    {
        var allPlayers = players.Where(p => p.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting)
            .ToList();

        // Queue priority is paid first, then arrival order. The separate two-win
        // break rule prevents the same winning team from playing indefinitely.
        return OrderByQueuePriority(allPlayers).ToList();
    }

    // ========== GAME CREATION ==========

    /// <summary>
    /// Creates the next game given the current set of players.
    /// Automatically selects teams, referee, and scorer based on priority rules.
    /// </summary>
    /// <param name="players">All players in the current session.</param>
    /// <param name="currentGameNumber">The sequential game number to assign.</param>
    /// <returns>A new Game object with teams and officials assigned.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not enough players for a game.</exception>
    public Game CreateNextGame(IEnumerable<Player> players, int currentGameNumber)
    {
        var queue = BuildEffectiveQueue(players);

        // Officials are assigned when available, but a 5v5 game can still start with ten players.
        int requiredPlayers = _settings.PlayersPerTeam * 2;
        if (queue.Count < requiredPlayers)
        {
            throw new InvalidOperationException(
                $"Insufficient players. Required: {requiredPlayers}, Available: {queue.Count}");
        }

        return CreateGame(currentGameNumber, queue.Take(_settings.PlayersPerTeam).ToList(),
            queue.Skip(_settings.PlayersPerTeam).Take(_settings.PlayersPerTeam).ToList(),
            queue.Skip(_settings.PlayersPerTeam * 2).ToList());
    }

    /// <summary>
    /// Creates the game that follows a completed game.  The winners remain together,
    /// while the next opponent keeps only as many losing players as are needed after
    /// players who were outside the game have been rotated in.
    /// </summary>
    public Game CreateNextGame(
        IEnumerable<Player> players,
        int currentGameNumber,
        Game previousGame,
        out List<Guid>? restingTeamPlayerIds)
    {
        restingTeamPlayerIds = null;
        if (previousGame.Status != GameStatus.Finished || previousGame.Winner is null)
            return CreateNextGame(players, currentGameNumber);

        var allPlayers = players.ToList();
        var winningTeam = previousGame.GetWinningTeam();
        var losingTeam = previousGame.GetLosingTeam();
        if (winningTeam is null || losingTeam is null)
            return CreateNextGame(allPlayers, currentGameNumber);

        var winners = PlayersInTeam(allPlayers, winningTeam);
        var losers = PlayersInTeam(allPlayers, losingTeam);
        var winnerIds = winners.Select(player => player.Id).ToHashSet();
        var loserIds = losers.Select(player => player.Id).ToHashSet();
        var outsidePlayers = allPlayers
            .Where(player =>
                !winnerIds.Contains(player.Id) &&
                !loserIds.Contains(player.Id) &&
                player.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting)
            .ToList();

        if (winners.Count != _settings.PlayersPerTeam || losers.Count != _settings.PlayersPerTeam)
            return CreateNextGame(allPlayers, currentGameNumber);

        // A full winning team that has won twice sits out one game when ten other
        // people are available.  Resetting its streak records the required break.
        if (ShouldGiveWinningTeamABreak(winners, outsidePlayers.Count))
        {
            restingTeamPlayerIds = winners.Select(player => player.Id).ToList();
            foreach (var winner in winners)
            {
                winner.ConsecutiveGames = 0;
                winner.CurrentStatus = PlayerStatus.Waiting;
            }

            return CreateNextGame(outsidePlayers, currentGameNumber);
        }

        // Rotate every player who was outside the previous game into the losing side,
        // up to a full team. Paid players have priority, then arrival order decides
        // who retains the remaining spots.
        // Example: with #11-13 outside, #6 and #7 stay, so the opponent is #6, #7,
        // #11, #12, #13; #8-10 become the next referee/scorer/waiting players.
        var incomingPlayers = OrderByQueuePriority(outsidePlayers)
            .Take(_settings.PlayersPerTeam)
            .ToList();
        var retainedLosers = OrderByQueuePriority(losers)
            .Take(_settings.PlayersPerTeam - incomingPlayers.Count)
            .ToList();
        var rotatingOpponent = retainedLosers.Concat(incomingPlayers).ToList();

        var reservePlayers = OrderByQueuePriority(losers)
            .Skip(retainedLosers.Count)
            .Concat(OrderByQueuePriority(outsidePlayers)
                .Where(player => !incomingPlayers.Contains(player)))
            .ToList();

        return previousGame.Winner == TeamSide.A
            ? CreateGame(currentGameNumber, winners, rotatingOpponent, reservePlayers)
            : CreateGame(currentGameNumber, rotatingOpponent, winners, reservePlayers);
    }

    /// <summary>
    /// Creates the game immediately after a two-win rest. The team that won the
    /// intervening game plays the team that completed its one-game rest.
    /// </summary>
    public Game CreateGameAgainstRestingTeam(
        IEnumerable<Player> players,
        int currentGameNumber,
        Game completedInterveningGame,
        IEnumerable<Guid> restingTeamPlayerIds)
    {
        var allPlayers = players.ToList();
        var restingIds = restingTeamPlayerIds.ToHashSet();
        var restingPlayers = allPlayers
            .Where(player => restingIds.Contains(player.Id) && player.CurrentStatus != PlayerStatus.Finished)
            .ToList();
        var interveningWinners = completedInterveningGame.GetWinningTeam() is { } winningTeam
            ? PlayersInTeam(allPlayers, winningTeam)
            : [];

        if (restingPlayers.Count != _settings.PlayersPerTeam ||
            interveningWinners.Count != _settings.PlayersPerTeam)
        {
            throw new InvalidOperationException("No se puede crear el juego de regreso: falta un equipo completo.");
        }

        var playingIds = restingIds.Union(interveningWinners.Select(player => player.Id)).ToHashSet();
        var reserves = allPlayers
            .Where(player =>
                !playingIds.Contains(player.Id) &&
                player.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting)
            .ToList();

        return CreateGame(currentGameNumber, restingPlayers, interveningWinners, reserves);
    }

    private Game CreateGame(
        int currentGameNumber,
        List<Player> teamAPlayers,
        List<Player> teamBPlayers,
        List<Player> reservePlayers)
    {
        if (teamAPlayers.Count != _settings.PlayersPerTeam || teamBPlayers.Count != _settings.PlayersPerTeam)
            throw new InvalidOperationException("Insufficient players to create two complete teams.");

        var game = new Game
        {
            Number = currentGameNumber,
            Status = GameStatus.Scheduled,
            StartDateTime = DateTime.Now
        };

        var teamA = new Team
        {
            Side = TeamSide.A,
            Game = game
        };
        teamA.SetPlayerIds(teamAPlayers.Select(p => p.Id).ToList());
        game.TeamA = teamA;
        game.TeamAId = teamA.Id;

        var teamB = new Team
        {
            Side = TeamSide.B,
            Game = game
        };
        teamB.SetPlayerIds(teamBPlayers.Select(p => p.Id).ToList());
        game.TeamB = teamB;
        game.TeamBId = teamB.Id;

        // The officials are always the latest arrivals among the players who are
        // not playing. The earlier reserve players remain in the waiting queue.
        var officialCount = _settings.RefereeCount + _settings.ScorerCount;
        var officials = reservePlayers
            .OrderByDescending(player => player.ArrivalNumber)
            .Take(officialCount)
            .OrderBy(player => player.ArrivalNumber)
            .ToList();

        if (_settings.RefereeCount > 0 && officials.Count > 0)
        {
            var referee = officials[0];
            game.RefereeId = referee.Id;
            game.Referee = referee;
            referee.CurrentStatus = PlayerStatus.Referee;
        }

        if (_settings.ScorerCount > 0 && officials.Count > _settings.RefereeCount)
        {
            var scorer = officials[_settings.RefereeCount];
            game.ScorerId = scorer.Id;
            game.Scorer = scorer;
            scorer.CurrentStatus = PlayerStatus.Scorer;
        }

        // Update all playing players' status
        foreach (var player in teamAPlayers.Concat(teamBPlayers))
        {
            player.CurrentStatus = PlayerStatus.Playing;
        }

        return game;
    }

    // ========== GAME RESULT PROCESSING ==========

    /// <summary>
    /// Processes the result of a finished game and returns the updated player states.
    /// This is called after a game ends to update statistics and determine next queue order.
    /// </summary>
    /// <param name="game">The completed game.</param>
    /// <param name="winner">Which team won (A or B).</param>
    /// <param name="allPlayers">All players in the session.</param>
    /// <returns>Updated players list with new statuses and statistics.</returns>
    public List<Player> FinishGame(Game game, TeamSide winner, List<Player> allPlayers)
    {
        if (game.Status == GameStatus.Finished)
            throw new InvalidOperationException("Game is already finished");

        game.Winner = winner;
        game.Status = GameStatus.Finished;
        game.EndDateTime = DateTime.Now;

        var losingTeam = winner == TeamSide.A ? game.TeamB! : game.TeamA!;
        var winningTeam = winner == TeamSide.A ? game.TeamA! : game.TeamB!;

        var winningPlayerIds = winningTeam.GetPlayerIds();
        var losingPlayerIds = losingTeam.GetPlayerIds();

        var winningPlayers = allPlayers.Where(p => winningPlayerIds.Contains(p.Id)).ToList();
        var losingPlayers = allPlayers.Where(p => losingPlayerIds.Contains(p.Id)).ToList();

        // Sort losing players by arrival order (for re-entry rules)
        losingPlayers.Sort((a, b) => a.ArrivalNumber.CompareTo(b.ArrivalNumber));

        // Update statistics for all players
        foreach (var player in winningPlayers)
        {
            player.GamesPlayed++;
            player.ConsecutiveGames++;
            player.LastGameNumber = game.Number;
            player.LastPlayedDateTime = DateTime.Now;
            player.CurrentStatus = PlayerStatus.Waiting;
        }

        foreach (var player in losingPlayers)
        {
            player.GamesPlayed++;
            player.ConsecutiveGames = 0; // Reset on loss
            player.LastGameNumber = game.Number;
            player.LastPlayedDateTime = DateTime.Now;
            player.CurrentStatus = PlayerStatus.LostWaiting;
        }

        // Officials return to the outside queue.  The following game rotates them
        // into the losing side before selecting the next officials.
        if (game.Referee != null)
        {
            game.Referee.CurrentStatus = PlayerStatus.Waiting;
            game.Referee.GamesWaiting++;
        }

        if (game.Scorer != null)
        {
            game.Scorer.CurrentStatus = PlayerStatus.Waiting;
            game.Scorer.GamesWaiting++;
        }

        return allPlayers;
    }

    /// <summary>
    /// Replaces a player who has left an active game with the highest-priority
    /// waiting player: paid first, then earliest arrival.
    /// </summary>
    public Player ReplacePlayerWhoLeft(Game game, Guid leavingPlayerId, List<Player> allPlayers)
    {
        if (game.Status == GameStatus.Finished)
            throw new InvalidOperationException("Cannot replace a player in a finished game.");

        var team = game.TeamA?.GetPlayerIds().Contains(leavingPlayerId) == true
            ? game.TeamA
            : game.TeamB?.GetPlayerIds().Contains(leavingPlayerId) == true
                ? game.TeamB
                : null;
        if (team is null)
            throw new InvalidOperationException("The selected player is not in the current game.");

        var leavingPlayer = allPlayers.FirstOrDefault(player => player.Id == leavingPlayerId)
            ?? throw new InvalidOperationException("The selected player could not be found.");
        // The current referee is the first replacement choice. When the referee
        // enters a team, the scorer advances to referee and the best waiting
        // player becomes scorer. If nobody is waiting, scorer stays unassigned.
        var replacement = game.RefereeId is Guid refereeId
            ? allPlayers.FirstOrDefault(player =>
                player.Id == refereeId && player.CurrentStatus == PlayerStatus.Referee)
            : null;
        if (replacement is not null)
        {
            var currentScorer = game.ScorerId is Guid scorerId
                ? allPlayers.FirstOrDefault(player =>
                    player.Id == scorerId && player.CurrentStatus == PlayerStatus.Scorer)
                : null;

            if (currentScorer is null)
            {
                game.RefereeId = null;
                game.Referee = null;
            }
            else
            {
                game.RefereeId = currentScorer.Id;
                game.Referee = currentScorer;
                currentScorer.CurrentStatus = PlayerStatus.Referee;

                // A replacement scorer is the earliest-arriving paid player.
                // When nobody waiting has paid, use the earliest arrival instead.
                var newScorer = allPlayers
                    .Where(player => player.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting)
                    .OrderByDescending(player => player.HasPaid)
                    .ThenBy(player => player.ArrivalNumber)
                    .FirstOrDefault();
                if (newScorer is null)
                {
                    game.ScorerId = null;
                    game.Scorer = null;
                }
                else
                {
                    game.ScorerId = newScorer.Id;
                    game.Scorer = newScorer;
                    newScorer.CurrentStatus = PlayerStatus.Scorer;
                }
            }
        }

        replacement ??= BuildEffectiveQueue(allPlayers).FirstOrDefault();
        if (replacement is null)
            throw new InvalidOperationException("There is no available referee or waiting player to replace them.");

        var playerIds = team.GetPlayerIds();
        var playerIndex = playerIds.IndexOf(leavingPlayerId);
        playerIds[playerIndex] = replacement.Id;
        team.SetPlayerIds(playerIds);

        leavingPlayer.CurrentStatus = PlayerStatus.Finished;
        leavingPlayer.ConsecutiveGames = 0;
        replacement.CurrentStatus = PlayerStatus.Playing;

        return replacement;
    }

    // ========== PLAYER REGISTRATION ==========

    /// <summary>
    /// Registers a new player in the system.
    /// Assigns an immutable arrival number and initializes all statistics.
    /// </summary>
    /// <param name="name">The player's name.</param>
    /// <param name="nextArrivalNumber">The next sequential arrival number to assign.</param>
    /// <returns>A new Player object ready to be added to the session.</returns>
    public Player RegisterPlayer(string name, int nextArrivalNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Player name cannot be empty");

        if (nextArrivalNumber < 1)
            throw new ArgumentException("Arrival number must be >= 1");

        return new Player
        {
            Name = name.Trim(),
            ArrivalNumber = nextArrivalNumber,
            ArrivalDateTime = DateTime.Now,
            CurrentStatus = PlayerStatus.Waiting,
            HasPaid = false,
            AmountPaid = 0,
            GamesPlayed = 0,
            GamesWaiting = 0,
            ConsecutiveGames = 0
        };
    }

    // ========== PAYMENT PROCESSING ==========

    /// <summary>
    /// Registers a payment for a player and updates their status if payment is sufficient.
    /// </summary>
    /// <param name="player">The player making the payment.</param>
    /// <param name="amount">The amount being paid (RD$).</param>
    /// <returns>True if player is now marked as having paid; false if payment insufficient.</returns>
    public bool RegisterPayment(Player player, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive");

        player.AmountPaid += amount;
        player.PaymentDateTime = DateTime.Now;

        if (!_settings.AllowPartialPayment)
        {
            // Must pay exact amount (or more)
            if (player.AmountPaid >= _settings.PlayerFee)
            {
                player.HasPaid = true;
                return true;
            }
        }
        else
        {
            // Any payment counts
            player.HasPaid = true;
            return true;
        }

        return false;
    }

    // ========== UTILITY METHODS ==========

    /// <summary>
    /// Gets the settings for this service.
    /// </summary>
    public BasketballQueueSettings GetSettings() => _settings;

    /// <summary>
    /// Calculates how many players can play in the next game.
    /// </summary>
    /// <param name="availablePlayerCount">Total available players.</param>
    /// <returns>The number of players that will actually play (0 if insufficient).</returns>
    public int CalculatePlayersForNextGame(int availablePlayerCount)
    {
        int requiredPlayers = _settings.PlayersPerTeam * 2;
        return availablePlayerCount >= requiredPlayers ? requiredPlayers : 0;
    }

    private static List<Player> PlayersInTeam(IEnumerable<Player> allPlayers, Team team) =>
        allPlayers.Where(player => team.GetPlayerIds().Contains(player.Id)).ToList();

    private IOrderedEnumerable<Player> OrderByQueuePriority(IEnumerable<Player> players) =>
        _settings.PaymentPriorityEnabled
            ? players.OrderByDescending(player => player.HasPaid)
                .ThenBy(player => player.ArrivalNumber)
            : players.OrderBy(player => player.ArrivalNumber);

    private bool ShouldGiveWinningTeamABreak(List<Player> winners, int outsidePlayerCount) =>
        _settings.PreventConsecutiveGames &&
        winners.All(player => player.ConsecutiveGames >= _settings.MaxConsecutiveGames) &&
        outsidePlayerCount >= _settings.PlayersPerTeam * 2;

    /// <summary>
    /// Validates that a game result is valid (correct team identifiers, etc).
    /// </summary>
    public bool ValidateGameResult(Game game, TeamSide winner)
    {
        if (game.Status == GameStatus.Finished)
            return false;

        if (game.TeamA == null || game.TeamB == null)
            return false;

        return true;
    }
}
