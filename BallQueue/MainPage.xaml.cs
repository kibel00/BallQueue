using BallQueue.Models;
using BallQueue.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BallQueue;

public partial class MainPage : ContentPage
{
    private BasketballViewModel? _viewModel;
    private bool _hasRestoredActiveSession;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = GetViewModel();
        if (viewModel is not null && !_hasRestoredActiveSession)
        {
            _hasRestoredActiveSession = true;
            await viewModel.RestoreActiveSessionAsync();
        }

        if (viewModel?.CurrentSession is not { } session)
            return;

        ActiveSessionLabel.Text = $"Sesión activa: {session.Name}";
        await RefreshSessionStatsAsync(session);
    }

    private async void OnCreateSession(object sender, EventArgs e)
    {
        var sessionName = SessionNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            StatusLabel.Text = "Escribe un nombre para la sesión";
            return;
        }

        var viewModel = GetViewModel();
        if (viewModel is null)
            return;

        viewModel.SessionName = sessionName;
        await viewModel.CreateSessionAsync();
        StatusLabel.Text = viewModel.StatusMessage;

        if (viewModel.CurrentSession is null)
            return;

        SessionNameEntry.Text = viewModel.SessionName;
        ActiveSessionLabel.Text = $"Sesión activa: {viewModel.CurrentSession.Name}";
        await RefreshSessionStatsAsync(viewModel.CurrentSession);
    }

    private async void OnManagePlayers(object sender, EventArgs e)
    {
        if (!HasActiveSession())
            return;

        var viewModel = GetViewModel()!;
        await Navigation.PushAsync(new Views.PlayersPage(
            viewModel.CreatePlayersViewModel(),
            viewModel.CurrentSession!.Id));
    }

    private async void OnViewGame(object sender, EventArgs e)
    {
        if (!HasActiveSession())
            return;

        var viewModel = GetViewModel()!;
        await Navigation.PushAsync(new Views.GamePage(
            viewModel.CreateGameViewModel(),
            viewModel.CurrentSession!.Id));
    }

    private async void OnViewHistory(object sender, EventArgs e)
    {
        var viewModel = GetViewModel();
        if (viewModel is null)
            return;

        await viewModel.LoadSessionsAsync();
        StatusLabel.Text = viewModel.StatusMessage;

        await Navigation.PushAsync(CreateHistoryPage(viewModel.Sessions));
    }

    private async void OnClearSession(object sender, EventArgs e)
    {
        if (!HasActiveSession())
            return;

        var confirmed = await DisplayAlertAsync(
            "Limpiar sesión",
            "Se eliminarán todos los jugadores, pagos y juegos de esta sesión. La sesión seguirá creada. ¿Deseas continuar?",
            "Sí, limpiar",
            "Cancelar");
        if (!confirmed)
            return;

        var viewModel = GetViewModel()!;
        await viewModel.ClearCurrentSessionAsync();
        StatusLabel.Text = viewModel.StatusMessage;
        if (viewModel.CurrentSession is not null)
        {
            ActiveSessionLabel.Text = $"Sesión activa: {viewModel.CurrentSession.Name}";
            await RefreshSessionStatsAsync(viewModel.CurrentSession);
        }
    }

    private async void OnDeleteSession(object sender, EventArgs e)
    {
        if (!HasActiveSession())
            return;

        var confirmed = await DisplayAlertAsync(
            "Eliminar sesión",
            "Esta acción eliminará permanentemente la sesión, sus jugadores, pagos y juegos. ¿Deseas continuar?",
            "Sí, eliminar",
            "Cancelar");
        if (!confirmed)
            return;

        var viewModel = GetViewModel()!;
        await viewModel.DeleteCurrentSessionAsync();
        StatusLabel.Text = viewModel.StatusMessage;
        SessionNameEntry.Text = string.Empty;
        ActiveSessionLabel.Text = "Sesión activa: (ninguna)";
        PlayersCountLabel.Text = "0";
        GamesCountLabel.Text = "0";
        WaitingCountLabel.Text = "0";
        PaidCountLabel.Text = "0";
    }

    private BasketballViewModel? GetViewModel()
    {
        if (_viewModel is not null)
            return _viewModel;

        var services = Handler?.MauiContext?.Services;
        if (services is null)
        {
            StatusLabel.Text = "Los servicios de la aplicación aún no están listos. Inténtalo de nuevo.";
            return null;
        }

        _viewModel = services.GetRequiredService<BasketballViewModel>();
        return _viewModel;
    }

    private bool HasActiveSession()
    {
        var viewModel = GetViewModel();
        if (viewModel?.CurrentSession is { IsActive: true })
            return true;

        StatusLabel.Text = "Crea una sesión activa antes de administrar jugadores o ver un juego.";
        return false;
    }

    private async Task RefreshSessionStatsAsync(Session session)
    {
        var viewModel = GetViewModel();
        if (viewModel is null)
            return;

        try
        {
            var stats = await viewModel.GetSessionStatsAsync(session.Id);
            PlayersCountLabel.Text = stats.Players.ToString();
            GamesCountLabel.Text = stats.Games.ToString();
            WaitingCountLabel.Text = stats.Waiting.ToString();
            PaidCountLabel.Text = stats.Paid.ToString();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"No se pudo cargar el resumen de la sesión: {ex.Message}";
        }
    }

    private static ContentPage CreateHistoryPage(IEnumerable<Session> sessions)
    {
        return new ContentPage
        {
            Title = "Historial de sesiones",
            Padding = 10,
            Content = new CollectionView
            {
                EmptyView = new Label
                {
                    Text = "Aún no se han creado sesiones.",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                },
                ItemsSource = sessions.ToList(),
                ItemTemplate = new DataTemplate(() =>
                {
                    var title = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
                    title.SetBinding(Label.TextProperty, nameof(Session.Name));

                    var date = new Label { FontSize = 12, Opacity = 0.7 };
                    date.SetBinding(Label.TextProperty, new Binding(
                        nameof(Session.StartDateTime),
                        stringFormat: "Inicio: {0:g}"));

                    var games = new Label { FontSize = 12 };
                    games.SetBinding(Label.TextProperty, new Binding(
                        nameof(Session.TotalGamesPlayed),
                        stringFormat: "Juegos disputados: {0}"));

                    return new Frame
                    {
                        BorderColor = Colors.LightGray,
                        CornerRadius = 8,
                        Margin = new Thickness(0, 5),
                        Padding = 12,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children = { title, date, games }
                        }
                    };
                })
            }
        };
    }
}
