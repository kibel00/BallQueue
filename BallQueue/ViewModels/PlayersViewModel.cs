using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BallQueue.Models;
using BallQueue.Services;

namespace BallQueue.ViewModels;

/// <summary>
/// ViewModel for player management page.
/// Handles player registration, payment, and queue display.
/// </summary>
public class PlayersViewModel : INotifyPropertyChanged
{
    private readonly BasketballQueueService _queueService;
    private readonly BasketballRepository _repository;
    private Session? _currentSession;
    private ObservableCollection<Player> _allPlayers = new();
    private ObservableCollection<Player> _effectiveQueue = new();
    private string _newPlayerName = string.Empty;
    private string _paymentAmount = string.Empty;
    private Player? _selectedPlayer;
    private string _statusMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlayersViewModel(BasketballQueueService queueService, BasketballRepository repository)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // ========== BINDABLE PROPERTIES ==========

    public string NewPlayerName
    {
        get => _newPlayerName;
        set { if (_newPlayerName != value) { _newPlayerName = value; OnPropertyChanged(); } }
    }

    public string PaymentAmount
    {
        get => _paymentAmount;
        set { if (_paymentAmount != value) { _paymentAmount = value; OnPropertyChanged(); } }
    }

    public Player? SelectedPlayer
    {
        get => _selectedPlayer;
        set { if (_selectedPlayer != value) { _selectedPlayer = value; OnPropertyChanged(); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Player> AllPlayers
    {
        get => _allPlayers;
        set { if (_allPlayers != value) { _allPlayers = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<Player> EffectiveQueue
    {
        get => _effectiveQueue;
        set { if (_effectiveQueue != value) { _effectiveQueue = value; OnPropertyChanged(); } }
    }

    // ========== COMMANDS ==========

    public Command RegisterPlayerCommand
    {
        get => new Command(async () => await RegisterPlayerAsync());
    }

    public Command RegisterPaymentCommand
    {
        get => new Command(async () => await RegisterPaymentAsync());
    }

    public Command RefreshCommand
    {
        get => new Command(async () => await RefreshPlayersAsync());
    }

    // ========== METHODS ==========

    /// <summary>
    /// Initializes the ViewModel with a session.
    /// </summary>
    public async Task InitializeAsync(Guid sessionId)
    {
        _currentSession = await _repository.GetSessionAsync(sessionId);
        await RefreshPlayersAsync();
    }

    /// <summary>
    /// Registers a new player in the current session.
    /// </summary>
    public async Task RegisterPlayerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPlayerName) || _currentSession == null)
        {
            StatusMessage = "Escribe el nombre del jugador";
            return;
        }

        try
        {
            var nextArrivalNumber = await _repository.GetNextArrivalNumberAsync(_currentSession.Id);
            var player = _queueService.RegisterPlayer(NewPlayerName, nextArrivalNumber);
            player.SessionId = _currentSession.Id;

            await _repository.SavePlayerAsync(player);
            NewPlayerName = string.Empty;
            StatusMessage = $"Jugador {player.Name} registrado como #{player.ArrivalNumber}";

            await RefreshPlayersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al registrar el jugador: {ex.Message}";
        }
    }

    /// <summary>
    /// Registers a payment for the selected player.
    /// </summary>
    public async Task RegisterPaymentAsync()
    {
        if (SelectedPlayer == null || string.IsNullOrWhiteSpace(PaymentAmount))
        {
            StatusMessage = "Selecciona un jugador e ingresa el monto del pago";
            return;
        }

        try
        {
            if (!decimal.TryParse(PaymentAmount, out var amount))
            {
                StatusMessage = "El monto del pago no es válido";
                return;
            }

            var fee = _queueService.GetSettings().PlayerFee;
            var paid = _queueService.RegisterPayment(SelectedPlayer, amount);

            var payment = new Models.Payment
            {
                PlayerId = SelectedPlayer.Id,
                SessionId = _currentSession!.Id,
                Amount = amount,
                PaymentDateTime = DateTime.Now
            };

            await _repository.SavePaymentAsync(payment);
            await _repository.SavePlayerAsync(SelectedPlayer);

            PaymentAmount = string.Empty;
            StatusMessage = paid 
                ? $"Pago recibido. {SelectedPlayer.Name} ya tiene prioridad para jugar."
                : $"Pago parcial recibido. Falta: RD${fee - SelectedPlayer.AmountPaid}";

            await RefreshPlayersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al procesar el pago: {ex.Message}";
        }
    }

    /// <summary>
    /// Refreshes all players and effective queue display.
    /// </summary>
    public async Task RefreshPlayersAsync()
    {
        if (_currentSession == null)
            return;

        try
        {
            var players = await _repository.GetPlayersInSessionAsync(_currentSession.Id);
            AllPlayers = new ObservableCollection<Player>(players);

            var queue = _queueService.BuildEffectiveQueue(players);
            EffectiveQueue = new ObservableCollection<Player>(queue);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al actualizar los jugadores: {ex.Message}";
        }
    }

    // ========== HELPER ==========

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
