using MatchMemoApp.ViewModels;

namespace MatchMemoApp.Views;

public partial class PlayerManagementPage : ContentPage
{
    private readonly PlayerManagementViewModel _viewModel;

    public PlayerManagementPage(PlayerManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearing();
    }
}