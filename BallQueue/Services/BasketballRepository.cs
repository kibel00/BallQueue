using BallQueue.Models;
using BallQueue.Data;
using Microsoft.EntityFrameworkCore;

namespace BallQueue.Services;

/// <summary>
/// Repository service for database persistence operations.
/// Handles CRUD operations for all basketball queue entities.
/// </summary>
public class BasketballRepository
{
    private readonly BasketballDbContext _dbContext;

    public BasketballRepository(BasketballDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // ========== SESSION OPERATIONS ==========

    /// <summary>
    /// Creates or updates a session in the database.
    /// </summary>
    public async Task<Session> SaveSessionAsync(Session session)
    {
        var existing = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == session.Id);
        if (existing == null)
        {
            _dbContext.Sessions.Add(session);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(session);
        }
        await _dbContext.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Retrieves a session by ID with all related data.
    /// </summary>
    public async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        return await _dbContext.Sessions
            .Include(s => s.Players)
            .Include(s => s.Games)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    /// <summary>
    /// Retrieves all sessions, ordered by most recent first.
    /// </summary>
    public async Task<List<Session>> GetAllSessionsAsync()
    {
        return await _dbContext.Sessions
            .OrderByDescending(s => s.StartDateTime)
            .ToListAsync();
    }

    // ========== PLAYER OPERATIONS ==========

    /// <summary>
    /// Adds a new player to the database.
    /// </summary>
    public async Task<Player> SavePlayerAsync(Player player)
    {
        var existing = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
        if (existing == null)
        {
            _dbContext.Players.Add(player);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(player);
        }
        await _dbContext.SaveChangesAsync();
        return player;
    }

    /// <summary>
    /// Saves multiple players in a single transaction.
    /// </summary>
    public async Task SavePlayersAsync(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            var existing = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == player.Id);
            if (existing == null)
            {
                _dbContext.Players.Add(player);
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(player);
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves all players in a session, ordered by arrival number.
    /// </summary>
    public async Task<List<Player>> GetPlayersInSessionAsync(Guid sessionId)
    {
        return await _dbContext.Players
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.ArrivalNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a specific player by ID.
    /// </summary>
    public async Task<Player?> GetPlayerAsync(Guid playerId)
    {
        return await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);
    }

    // ========== GAME OPERATIONS ==========

    /// <summary>
    /// Saves a new game or updates an existing one.
    /// </summary>
    public async Task<Game> SaveGameAsync(Game game)
    {
        var existing = await _dbContext.Games.FirstOrDefaultAsync(g => g.Id == game.Id);
        if (existing == null)
        {
            _dbContext.Games.Add(game);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(game);
        }
        await _dbContext.SaveChangesAsync();

        await SaveTeamsAsync(game);
        return game;
    }

    /// <summary>
    /// Retrieves all games in a session.
    /// </summary>
    public async Task<List<Game>> GetGamesInSessionAsync(Guid sessionId)
    {
        var games = await _dbContext.Games
            .Where(g => g.SessionId == sessionId)
            .Include(g => g.Referee)
            .Include(g => g.Scorer)
            .OrderBy(g => g.Number)
            .ToListAsync();

        await PopulateTeamsAsync(games);
        return games;
    }

    /// <summary>
    /// Retrieves a specific game by ID.
    /// </summary>
    public async Task<Game?> GetGameAsync(Guid gameId)
    {
        var game = await _dbContext.Games
            .Include(g => g.Referee)
            .Include(g => g.Scorer)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is not null)
            await PopulateTeamsAsync([game]);

        return game;
    }

    // ========== TEAM OPERATIONS ==========

    /// <summary>
    /// Saves a team (usually called as part of game creation).
    /// </summary>
    public async Task<Team> SaveTeamAsync(Team team)
    {
        var existing = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Id == team.Id);
        if (existing == null)
        {
            _dbContext.Teams.Add(team);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(team);
        }
        await _dbContext.SaveChangesAsync();
        return team;
    }

    private async Task SaveTeamsAsync(Game game)
    {
        if (game.TeamA is not null)
        {
            game.TeamA.GameId = game.Id;
            await SaveTeamAsync(game.TeamA);
        }

        if (game.TeamB is not null)
        {
            game.TeamB.GameId = game.Id;
            await SaveTeamAsync(game.TeamB);
        }
    }

    private async Task PopulateTeamsAsync(IEnumerable<Game> games)
    {
        var gameList = games.ToList();
        if (gameList.Count == 0)
            return;

        var gameIds = gameList.Select(game => game.Id).ToList();
        var teams = await _dbContext.Teams
            .Where(team => gameIds.Contains(team.GameId))
            .ToListAsync();

        foreach (var game in gameList)
        {
            game.TeamA = teams.FirstOrDefault(team => team.GameId == game.Id && team.Side == Enums.TeamSide.A);
            game.TeamB = teams.FirstOrDefault(team => team.GameId == game.Id && team.Side == Enums.TeamSide.B);
        }
    }

    // ========== PAYMENT OPERATIONS ==========

    /// <summary>
    /// Records a payment in the database.
    /// </summary>
    public async Task<Payment> SavePaymentAsync(Payment payment)
    {
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();
        return payment;
    }

    /// <summary>
    /// Retrieves all payments for a specific player.
    /// </summary>
    public async Task<List<Payment>> GetPlayerPaymentsAsync(Guid playerId)
    {
        return await _dbContext.Payments
            .Where(p => p.PlayerId == playerId)
            .OrderByDescending(p => p.PaymentDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all payments in a session.
    /// </summary>
    public async Task<List<Payment>> GetSessionPaymentsAsync(Guid sessionId)
    {
        return await _dbContext.Payments
            .Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.PaymentDateTime)
            .ToListAsync();
    }

    // ========== UTILITY OPERATIONS ==========

    /// <summary>
    /// Gets the next arrival number for a new player in a session.
    /// </summary>
    public async Task<int> GetNextArrivalNumberAsync(Guid sessionId)
    {
        var maxNumber = await _dbContext.Players
            .Where(p => p.SessionId == sessionId)
            .MaxAsync(p => (int?)p.ArrivalNumber) ?? 0;

        return maxNumber + 1;
    }

    /// <summary>
    /// Gets the next game number for a session.
    /// </summary>
    public async Task<int> GetNextGameNumberAsync(Guid sessionId)
    {
        var maxNumber = await _dbContext.Games
            .Where(g => g.SessionId == sessionId)
            .MaxAsync(g => (int?)g.Number) ?? 0;

        return maxNumber + 1;
    }

    /// <summary>
    /// Deletes a session and all its related data.
    /// </summary>
    public async Task<Session> ClearSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
            throw new InvalidOperationException("Session not found.");

        await RemoveSessionContentAsync(sessionId);
        session.TotalGamesPlayed = 0;
        session.EndDateTime = null;
        await _dbContext.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Permanently deletes one session and all of its games, teams, players, and payments.
    /// </summary>
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
            return;

        await RemoveSessionContentAsync(sessionId);
        _dbContext.Sessions.Remove(session);
        await _dbContext.SaveChangesAsync();
    }

    private async Task RemoveSessionContentAsync(Guid sessionId)
    {
        var gameIds = await _dbContext.Games
            .Where(game => game.SessionId == sessionId)
            .Select(game => game.Id)
            .ToListAsync();

        var teams = await _dbContext.Teams
            .Where(team => gameIds.Contains(team.GameId))
            .ToListAsync();
        var payments = await _dbContext.Payments
            .Where(payment => payment.SessionId == sessionId)
            .ToListAsync();
        var games = await _dbContext.Games
            .Where(game => game.SessionId == sessionId)
            .ToListAsync();
        var players = await _dbContext.Players
            .Where(player => player.SessionId == sessionId)
            .ToListAsync();

        _dbContext.Teams.RemoveRange(teams);
        _dbContext.Payments.RemoveRange(payments);
        _dbContext.Games.RemoveRange(games);
        _dbContext.Players.RemoveRange(players);
    }

    /// <summary>
    /// Clears all entities from the database (for testing).
    /// WARNING: This deletes all data!
    /// </summary>
    public async Task ClearAllAsync()
    {
        _dbContext.Payments.RemoveRange(_dbContext.Payments);
        _dbContext.Games.RemoveRange(_dbContext.Games);
        _dbContext.Teams.RemoveRange(_dbContext.Teams);
        _dbContext.Players.RemoveRange(_dbContext.Players);
        _dbContext.Sessions.RemoveRange(_dbContext.Sessions);
        await _dbContext.SaveChangesAsync();
    }
}
