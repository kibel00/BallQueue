using BallQueue.Models;
using BallQueue.ViewModels;

namespace BallQueue.Views;

public partial class PlayersPage : ContentPage
{
    private readonly PlayersViewModel _viewModel;
    private readonly Guid _sessionId;
    private bool _isInitialized;

    public PlayersPage(PlayersViewModel viewModel, Guid sessionId)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized)
            return;

        try
        {
            await _viewModel.InitializeAsync(_sessionId);
            RefreshPlayersList();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("No se pudieron cargar los jugadores", ex.Message, "Aceptar");
        }
    }

    private async void OnRegisterPlayer(object sender, EventArgs e)
    {
        var playerName = PlayerNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            await DisplayAlertAsync("Error", "Escribe el nombre del jugador", "Aceptar");
            return;
        }

        _viewModel.NewPlayerName = playerName;
        await _viewModel.RegisterPlayerAsync();
        RefreshPlayersList();

        if (_viewModel.NewPlayerName.Length == 0)
        {
            PlayerNameEntry.Text = string.Empty;
            await DisplayAlertAsync("Correcto", _viewModel.StatusMessage, "Aceptar");
            return;
        }

        await DisplayAlertAsync("No se pudo registrar el jugador", _viewModel.StatusMessage, "Aceptar");
    }

    private async void OnPaymentClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Player player })
            return;

        _viewModel.SelectedPlayer = player;
        _viewModel.PaymentAmount = "100";
        await _viewModel.RegisterPaymentAsync();
        RefreshPlayersList();

        await DisplayAlertAsync("Pago", _viewModel.StatusMessage, "Aceptar");
    }

    private void RefreshPlayersList() => PlayersCollectionView.ItemsSource = _viewModel.AllPlayers;
}
