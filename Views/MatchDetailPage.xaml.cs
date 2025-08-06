using MatchMemoApp.ViewModels;

namespace MatchMemoApp.Views;

public partial class MatchDetailPage : ContentPage
{
    private readonly MatchDetailViewModel _viewModel;

    public MatchDetailPage(MatchDetailViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}