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

        public ObservableCollection<MatchWrapper> MatchWrappers { get; } = new();

        [ObservableProperty]
        private bool isSelectionMode = false;

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

                if (MatchWrappers.Count != 0)
                    MatchWrappers.Clear();

                foreach (var match in matches)
                    MatchWrappers.Add(new MatchWrapper(match));
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "なし";
                var stackTrace = ex.StackTrace ?? "なし";
                await Shell.Current.DisplayAlert("エラー",
                    $"エラー (試合一覧の取得に失敗しました。): {ex.Message}\n" +
                    $"内部例外: {innerException}\n" +
                    $"スタックトレース: {stackTrace}", "OK");
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
        async Task GoToMatchAsync(MatchWrapper matchWrapper)
        {
            if (matchWrapper == null)
                return;

            // 選択モードの場合は選択/非選択を切り替え
            if (IsSelectionMode)
            {
                matchWrapper.IsSelected = !matchWrapper.IsSelected;
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(MatchDetailPage)}?MatchId={matchWrapper.Match.Id}");
        }

        [RelayCommand]
        void StartSelectionMode(MatchWrapper matchWrapper)
        {
            IsSelectionMode = true;

            // 全ての選択をクリア
            foreach (var wrapper in MatchWrappers)
            {
                wrapper.IsSelected = false;
            }

            // 長押しされた項目を選択
            if (matchWrapper != null)
            {
                matchWrapper.IsSelected = true;
            }
        }

        [RelayCommand]
        void CancelSelection()
        {
            IsSelectionMode = false;

            // 全ての選択をクリア
            foreach (var wrapper in MatchWrappers)
            {
                wrapper.IsSelected = false;
            }
        }

        [RelayCommand]
        async Task DeleteSelectedMatchesAsync()
        {
            var selectedMatches = MatchWrappers.Where(w => w.IsSelected).ToList();

            if (selectedMatches.Count == 0)
            {
                await Shell.Current.DisplayAlert("エラー", "削除する試合が選択されていません。", "OK");
                return;
            }

            string message = selectedMatches.Count == 1
                ? "選択された試合を削除しますか？この操作は元に戻せません。"
                : $"{selectedMatches.Count}件の試合を削除しますか？この操作は元に戻せません。";

            bool confirmed = await Shell.Current.DisplayAlert("確認", message, "OK", "キャンセル");

            if (!confirmed)
                return;

            try
            {
                IsBusy = true;

                var matchIds = selectedMatches.Select(w => w.Match.Id).ToList();
                await _databaseService.DeleteMatchesAsync(matchIds);

                // UI更新
                foreach (var matchWrapper in selectedMatches)
                {
                    MatchWrappers.Remove(matchWrapper);
                }

                CancelSelection();

                string completionMessage = selectedMatches.Count == 1
                    ? "試合を削除しました。"
                    : $"{selectedMatches.Count}件の試合を削除しました。";

                await Shell.Current.DisplayAlert("完了", completionMessage, "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"削除に失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public int SelectedCount => MatchWrappers.Count(w => w.IsSelected);

        public async Task OnAppearing()
        {
            await GetMatchesAsync();
        }
    }
}