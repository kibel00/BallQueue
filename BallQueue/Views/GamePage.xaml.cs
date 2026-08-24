using BallQueue.Enums;
using BallQueue.Models;
using BallQueue.ViewModels;

namespace BallQueue.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private readonly Guid _sessionId;

    public GamePage(GameViewModel viewModel, Guid sessionId)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGameAsync();
    }

    private async void OnTeamAWon(object sender, EventArgs e) =>
        await FinishGameAsync(TeamSide.A);

    private async void OnTeamBWon(object sender, EventArgs e) =>
        await FinishGameAsync(TeamSide.B);

    private async void OnRefresh(object sender, EventArgs e) =>
        await LoadGameAsync();

    private async void OnSpanishRules(object sender, EventArgs e) =>
        await Navigation.PushAsync(new RulesPage());

    private async void OnBack(object sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async Task LoadGameAsync()
    {
        await _viewModel.LoadCurrentGameAsync(_sessionId);
        UpdateDisplay();
    }

    private async Task FinishGameAsync(TeamSide winner)
    {
        if (!_viewModel.HasCurrentGame)
            return;

        await _viewModel.FinishGameAsync(winner);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var hasGame = _viewModel.HasCurrentGame;

        GameNumberLabel.Text = hasGame ? _viewModel.GameNumber.ToString() : "—";
        PopulateTeamPlayerList(TeamAList, hasGame ? _viewModel.TeamAPlayers : Array.Empty<Player>());
        PopulateTeamPlayerList(TeamBList, hasGame ? _viewModel.TeamBPlayers : Array.Empty<Player>());
        PopulatePlayerList(WaitingList, hasGame ? _viewModel.WaitingPlayers : Array.Empty<Player>(), includeArrivalNumber: true);
        RefereeLabel.Text = _viewModel.Referee?.Name ?? "(Sin asignar)";
        ScorerLabel.Text = _viewModel.Scorer?.Name ?? "(Sin asignar)";
        TeamAWinButton.IsEnabled = hasGame;
        TeamBWinButton.IsEnabled = hasGame;
        StatusLabel.Text = _viewModel.StatusMessage;
        StatusLabel.TextColor = hasGame ? Colors.DarkGreen : Colors.IndianRed;
    }

    private static void PopulatePlayerList(
        VerticalStackLayout container,
        IEnumerable<Player> players,
        bool includeArrivalNumber = false)
    {
        container.Children.Clear();

        foreach (var player in players)
        {
            container.Children.Add(new Label
            {
                Text = includeArrivalNumber ? $"#{player.ArrivalNumber}  {player.Name}" : player.Name,
                FontSize = 12,
                Margin = new Thickness(5, 2)
            });
        }
    }

    private void PopulateTeamPlayerList(VerticalStackLayout container, IEnumerable<Player> players)
    {
        container.Children.Clear();

        foreach (var player in players)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            row.Add(new Label
            {
                Text = player.Name,
                FontSize = 12,
                Margin = new Thickness(5, 2)
            });

            var leaveButton = new Button
            {
                Text = "Salir",
                FontSize = 10,
                Padding = new Thickness(7, 2),
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.IndianRed,
                CommandParameter = player
            };
            leaveButton.Clicked += OnPlayerLeft;
            row.Add(leaveButton, 1, 0);
            container.Children.Add(row);
        }
    }

    private async void OnPlayerLeft(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Player player })
            return;

        var confirmed = await DisplayAlertAsync(
            "Sustituir jugador",
            $"¿Confirmas que {player.Name} sale del juego? Entrará el jugador de espera con mayor prioridad.",
            "Sí, sustituir",
            "Cancelar");
        if (!confirmed)
            return;

        await _viewModel.ReplacePlayerWhoLeftAsync(player.Id);
        UpdateDisplay();
    }
}
