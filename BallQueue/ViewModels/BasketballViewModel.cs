using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BallQueue.Enums;
using BallQueue.Models;
using BallQueue.Services;

namespace BallQueue.ViewModels;

/// <summary>
/// Main ViewModel for the basketball queue application.
/// Coordinates between sessions, players, and games.
/// </summary>
public class BasketballViewModel : INotifyPropertyChanged
{
    private readonly BasketballRepository _repository;
    private readonly BasketballQueueService _queueService;
    private Session? _currentSession;
    private ObservableCollection<Session> _sessions = new();
    private string _sessionName = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isSessionActive = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BasketballViewModel(BasketballRepository repository, BasketballQueueService queueService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
    }

    // ========== BINDABLE PROPERTIES ==========

    public Session? CurrentSession
    {
        get => _currentSession;
        set { if (_currentSession != value) { _currentSession = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Session> Sessions
    {
        get => _sessions;
        set { if (_sessions != value) { _sessions = value; OnPropertyChanged(); } }
    }

    public string SessionName
    {
        get => _sessionName;
        set { if (_sessionName != value) { _sessionName = value; OnPropertyChanged(); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public bool IsSessionActive
    {
        get => _isSessionActive;
        set { if (_isSessionActive != value) { _isSessionActive = value; OnPropertyChanged(); } }
    }

    // ========== COMMANDS ==========

    public Command CreateSessionCommand
    {
        get => new Command(async () => await CreateSessionAsync());
    }

    public Command StartSessionCommand
    {
        get => new Command(async () => await StartSessionAsync());
    }

    public Command EndSessionCommand
    {
        get => new Command(async () => await EndSessionAsync());
    }

    public Command LoadSessionsCommand
    {
        get => new Command(async () => await LoadSessionsAsync());
    }

    // ========== METHODS ==========

    /// <summary>
    /// Loads all previous sessions from the database.
    /// </summary>
    public async Task LoadSessionsAsync()
    {
        try
        {
            var sessions = await _repository.GetAllSessionsAsync();
            Sessions = new ObservableCollection<Session>(sessions);
            StatusMessage = $"Se cargaron {sessions.Count} sesiones";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar las sesiones: {ex.Message}";
        }
    }

    /// <summary>
    /// Restores the most recently created session that has not been ended.
    /// </summary>
    public async Task RestoreActiveSessionAsync()
    {
        try
        {
            var sessions = await _repository.GetAllSessionsAsync();
            CurrentSession = sessions.FirstOrDefault(session => session.IsActive);
            IsSessionActive = CurrentSession is not null;

            if (CurrentSession is not null)
                StatusMessage = $"Sesión {CurrentSession.Name} restaurada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al restaurar la sesión activa: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a new basketball session.
    /// </summary>
    public async Task CreateSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionName))
        {
            StatusMessage = "Escribe un nombre para la sesión";
            return;
        }

        try
        {
            var session = new Session
            {
                Name = SessionName,
                StartDateTime = DateTime.Now
            };

            CurrentSession = await _repository.SaveSessionAsync(session);
            IsSessionActive = true;
            SessionName = string.Empty;
            StatusMessage = $"Sesión {session.Name} creada";

            await LoadSessionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al crear la sesión: {ex.Message}";
        }
    }

    /// <summary>
    /// Starts (activates) an existing session.
    /// </summary>
    public async Task StartSessionAsync()
    {
        if (CurrentSession == null)
        {
            StatusMessage = "Primero selecciona una sesión";
            return;
        }

        try
        {
            CurrentSession.EndDateTime = null;
            CurrentSession = await _repository.SaveSessionAsync(CurrentSession);
            IsSessionActive = true;
            StatusMessage = $"Sesión {CurrentSession.Name} iniciada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al iniciar la sesión: {ex.Message}";
        }
    }

    /// <summary>
    /// Ends the current session.
    /// </summary>
    public async Task EndSessionAsync()
    {
        if (CurrentSession == null)
        {
            StatusMessage = "No hay una sesión activa";
            return;
        }

        try
        {
            CurrentSession.EndDateTime = DateTime.Now;
            CurrentSession = await _repository.SaveSessionAsync(CurrentSession);
            IsSessionActive = false;
            StatusMessage = $"Sesión finalizada. Juegos totales: {CurrentSession.TotalGamesPlayed}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al finalizar la sesión: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets references to child ViewModels for complex pages.
    /// </summary>
    public PlayersViewModel CreatePlayersViewModel() => 
        new PlayersViewModel(_queueService, _repository);

    public GameViewModel CreateGameViewModel() => 
        new GameViewModel(_queueService, _repository);

    /// <summary>
    /// Gets the latest dashboard counters for a session from persisted data.
    /// </summary>
    public async Task<(int Players, int Games, int Waiting, int Paid)> GetSessionStatsAsync(Guid sessionId)
    {
        var players = await _repository.GetPlayersInSessionAsync(sessionId);
        var games = await _repository.GetGamesInSessionAsync(sessionId);

        return (
            Players: players.Count,
            Games: games.Count,
            Waiting: players.Count(player => player.CurrentStatus is PlayerStatus.Waiting or PlayerStatus.LostWaiting),
            Paid: players.Count(player => player.HasPaid));
    }

    /// <summary>
    /// Removes all games, players, and payments while keeping the current session.
    /// </summary>
    public async Task ClearCurrentSessionAsync()
    {
        if (CurrentSession is null)
        {
            StatusMessage = "No hay una sesión activa para limpiar";
            return;
        }

        try
        {
            CurrentSession = await _repository.ClearSessionAsync(CurrentSession.Id);
            IsSessionActive = true;
            await LoadSessionsAsync();
            StatusMessage = "La sesión fue limpiada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al limpiar la sesión: {ex.Message}";
        }
    }

    /// <summary>
    /// Permanently deletes the current session and its content.
    /// </summary>
    public async Task DeleteCurrentSessionAsync()
    {
        if (CurrentSession is null)
        {
            StatusMessage = "No hay una sesión activa para eliminar";
            return;
        }

        try
        {
            await _repository.DeleteSessionAsync(CurrentSession.Id);
            CurrentSession = null;
            IsSessionActive = false;
            SessionName = string.Empty;
            await LoadSessionsAsync();
            StatusMessage = "La sesión fue eliminada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al eliminar la sesión: {ex.Message}";
        }
    }

    // ========== HELPER ==========

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
