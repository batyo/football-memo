using MatchMemoApp.ViewModels;

namespace MatchMemoApp.Views;

public partial class NewMatchPage : ContentPage
{
    private readonly NewMatchViewModel _viewModel;

    public NewMatchPage(NewMatchViewModel viewModel)
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