using CommunityToolkit.Mvvm.ComponentModel;

namespace MatchMemoApp.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        bool isBusy;

        [ObservableProperty]
        string title = string.Empty;
    }
}