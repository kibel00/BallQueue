namespace BallQueue.Views;

public partial class RulesPage : ContentPage
{
    public RulesPage()
    {
        InitializeComponent();
    }

    private async void OnBack(object sender, EventArgs e) =>
        await Navigation.PopAsync();
}
