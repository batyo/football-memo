using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using MatchMemoApp.Views;
using System.Collections.ObjectModel;

namespace MatchMemoApp.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<Match> Matches { get; } = new();

        public MainViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "試合メモ";
        }

        [RelayCommand]
        async Task GetMatchesAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var matches = await _databaseService.GetMatchesAsync();

                if (Matches.Count != 0)
                    Matches.Clear();

                foreach (var match in matches)
                    Matches.Add(match);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"試合一覧の取得に失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task CreateNewMatchAsync()
        {
            await Shell.Current.GoToAsync(nameof(NewMatchPage));
        }

        [RelayCommand]
        async Task GoToPlayerManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(PlayerManagementPage));
        }

        [RelayCommand]
        async Task GoToMatchAsync(Match match)
        {
            if (match == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(MatchDetailPage)}?MatchId={match.Id}");
        }

        public async Task OnAppearing()
        {
            await GetMatchesAsync();
        }
    }
}