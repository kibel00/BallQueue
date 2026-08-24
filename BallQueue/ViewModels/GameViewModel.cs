using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BallQueue.Enums;
using BallQueue.Models;
using BallQueue.Services;

namespace BallQueue.ViewModels;

/// <summary>
/// ViewModel for the main basketball game display page.
/// Manages the current game state and provides commands for game operations.
/// Implements INotifyPropertyChanged for MVVM binding.
/// </summary>
public class GameViewModel : INotifyPropertyChanged
{
    private readonly BasketballQueueService _queueService;
    private readonly BasketballRepository _repository;
    private Game? _currentGame;
    private Session? _currentSession;
    private ObservableCollection<Player> _teamAPlayers = new();
    private ObservableCollection<Player> _teamBPlayers = new();
    private Player? _referee;
    private Player? _scorer;
    private ObservableCollection<Player> _waitingPlayers = new();
    private int _gameNumber;
    private string _teamAScore = "0";
    private string _teamBScore = "0";
    private string _statusMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public GameViewModel(BasketballQueueService queueService, BasketballRepository repository)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // ========== BINDABLE PROPERTIES ==========

    public int GameNumber
    {
        get => _gameNumber;
        set { if (_gameNumber != value) { _gameNumber = value; OnPropertyChanged(); } }
    }

    public string TeamAScore
    {
        get => _teamAScore;
        set { if (_teamAScore != value) { _teamAScore = value; OnPropertyChanged(); } }
    }

    public string TeamBScore
    {
        get => _teamBScore;
        set { if (_teamBScore != value) { _teamBScore = value; OnPropertyChanged(); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public bool HasCurrentGame => _currentGame is not null;

    public ObservableCollection<Player> TeamAPlayers
    {
        get => _teamAPlayers;
        set { if (_teamAPlayers != value) { _teamAPlayers = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Player> TeamBPlayers
    {
        get => _teamBPlayers;
        set { if (_teamBPlayers != value) { _teamBPlayers = value; OnPropertyChanged(); } }
    }

    public Player? Referee
    {
        get => _referee;
        set { if (_referee != value) { _referee = value; OnPropertyChanged(); } }
    }

    public Player? Scorer
    {
        get => _scorer;
        set { if (_scorer != value) { _scorer = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Player> WaitingPlayers
    {
        get => _waitingPlayers;
        set { if (_waitingPlayers != value) { _waitingPlayers = value; OnPropertyChanged(); } }
    }

    // ========== COMMANDS ==========

    public Command FinishGameCommand
    {
        get => new Command(async () => await FinishGameAsync());
    }

    public Command RefreshCommand
    {
        get => new Command(async () => await RefreshGameDisplayAsync());
    }

    // ========== METHODS ==========

    /// <summary>
    /// Loads the current game and displays its details.
    /// </summary>
    public async Task LoadCurrentGameAsync(Guid sessionId)
    {
        try
        {
            StatusMessage = string.Empty;
            _currentSession = await _repository.GetSessionAsync(sessionId);
            if (_currentSession == null)
            {
                StatusMessage = "No se pudo encontrar la sesión seleccionada.";
                return;
            }

            var games = await _repository.GetGamesInSessionAsync(sessionId);
            _currentGame = games.LastOrDefault(game => game.Status is GameStatus.Scheduled or GameStatus.InProgress);

            if (_currentGame == null)
            {
                var players = await _repository.GetPlayersInSessionAsync(sessionId);
                var nextGameNumber = await _repository.GetNextGameNumberAsync(sessionId);
                _currentGame = _queueService.CreateNextGame(players, nextGameNumber);
                _currentGame.SessionId = sessionId;
                await _repository.SaveGameAsync(_currentGame);
                await _repository.SavePlayersAsync(players);
            }

            await RefreshGameDisplayAsync();
            StatusMessage = $"El juego #{_currentGame.Number} está listo.";
        }
        catch (Exception ex)
        {
            _currentGame = null;
            StatusMessage = $"No se pudo preparar el juego: {ex.Message}";
        }
    }

    /// <summary>
    /// Refreshes the UI with current game state.
    /// </summary>
    private async Task RefreshGameDisplayAsync()
    {
        if (_currentGame == null || _currentSession == null)
            return;

        try
        {
            GameNumber = _currentGame.Number;

            // Load teams
            var allPlayers = await _repository.GetPlayersInSessionAsync(_currentSession.Id);

            if (_currentGame.TeamA != null)
            {
                var teamAIds = _currentGame.TeamA.GetPlayerIds();
                var teamAPlayers = allPlayers.Where(p => teamAIds.Contains(p.Id)).ToList();
                TeamAPlayers = new ObservableCollection<Player>(teamAPlayers);
            }

            if (_currentGame.TeamB != null)
            {
                var teamBIds = _currentGame.TeamB.GetPlayerIds();
                var teamBPlayers = allPlayers.Where(p => teamBIds.Contains(p.Id)).ToList();
                TeamBPlayers = new ObservableCollection<Player>(teamBPlayers);
            }

            // Load officials and waiting
            Referee = _currentGame.RefereeId.HasValue
                ? allPlayers.FirstOrDefault(player => player.Id == _currentGame.RefereeId)
                : null;

            Scorer = _currentGame.ScorerId.HasValue
                ? allPlayers.FirstOrDefault(player => player.Id == _currentGame.ScorerId)
                : null;

            var waiting = allPlayers
                .Where(p => p.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting)
                .ToList();
            WaitingPlayers = new ObservableCollection<Player>(waiting);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh error: {ex.Message}");
        }
    }

    /// <summary>
    /// Finishes current game with winner and creates next game.
    /// </summary>
    public async Task FinishGameAsync(TeamSide winner = TeamSide.A)
    {
        if (_currentGame == null || _currentSession == null)
            return;

        try
        {
            var allPlayers = await _repository.GetPlayersInSessionAsync(_currentSession.Id);
            _queueService.FinishGame(_currentGame, winner, allPlayers);

            await _repository.SaveGameAsync(_currentGame);
            await _repository.SavePlayersAsync(allPlayers);

            _currentSession.TotalGamesPlayed++;
            await _repository.SaveSessionAsync(_currentSession);

            // Create next game
            var nextGameNumber = _currentGame.Number + 1;
            _currentGame = _queueService.CreateNextGame(allPlayers, nextGameNumber, _currentGame);
            _currentGame.SessionId = _currentSession.Id;
            await _repository.SaveGameAsync(_currentGame);
            await _repository.SavePlayersAsync(allPlayers);

            await RefreshGameDisplayAsync();
            StatusMessage = $"El juego #{nextGameNumber - 1} terminó. El juego #{nextGameNumber} está listo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo finalizar el juego: {ex.Message}";
        }
    }

    /// <summary>
    /// Removes a player who leaves the current game and immediately substitutes
    /// the next eligible waiting player.
    /// </summary>
    public async Task ReplacePlayerWhoLeftAsync(Guid leavingPlayerId)
    {
        if (_currentGame == null || _currentSession == null)
            return;

        try
        {
            var allPlayers = await _repository.GetPlayersInSessionAsync(_currentSession.Id);
            var leavingPlayer = allPlayers.FirstOrDefault(player => player.Id == leavingPlayerId);
            var replacement = _queueService.ReplacePlayerWhoLeft(_currentGame, leavingPlayerId, allPlayers);

            await _repository.SaveGameAsync(_currentGame);
            await _repository.SavePlayersAsync(allPlayers);
            await RefreshGameDisplayAsync();

            StatusMessage = $"{leavingPlayer?.Name ?? "El jugador"} salió. {replacement.Name} entró al equipo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo sustituir al jugador: {ex.Message}";
        }
    }

    // ========== HELPER ==========

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
