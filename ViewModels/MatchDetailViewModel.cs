using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace MatchMemoApp.ViewModels
{
    [QueryProperty(nameof(MatchId), "MatchId")]
    public partial class MatchDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private System.Timers.Timer _matchTimer;

        [ObservableProperty]
        int matchId;

        [ObservableProperty]
        MatchMemoApp.Models.Match currentMatch;

        [ObservableProperty]
        string matchTimeDisplay = "00:00";

        [ObservableProperty]
        bool isTimerRunning = false;

        [ObservableProperty]
        Player selectedPlayer;

        [ObservableProperty]
        string memoText = string.Empty;

        [ObservableProperty]
        bool isRealTimeMode = true;

        [ObservableProperty]
        bool isHomeTeamSelected = true;

        // エディター関連の新しいプロパティ
        [ObservableProperty]
        bool isEditorVisible = false;

        [ObservableProperty]
        bool isEditorFocused = false;

        // フィールド選手のコレクション
        public ObservableCollection<FieldPlayerView> HomeTeamPlayersOnField { get; } = new();
        public ObservableCollection<FieldPlayerView> AwayTeamPlayersOnField { get; } = new();

        // 既存のコレクション（参考用に残す）
        public ObservableCollection<PlayerWithMemos> HomeTeamPlayers { get; } = new();
        public ObservableCollection<PlayerWithMemos> AwayTeamPlayers { get; } = new();
        public ObservableCollection<Memo> CurrentPlayerMemos { get; } = new();
        public ObservableCollection<string> MemoTemplates { get; } = new()
        {
            "好調なプレー",
            "パスミス",
            "シュート",
            "ナイスセーブ",
            "ファウル",
            "イエローカード",
            "交代",
            "オフサイド",
            "コーナーキック",
            "フリーキック",
            "ドリブル突破",
            "クロス",
            "ヘディング",
            "タックル",
            "インターセプト"
        };

        public MatchDetailViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "試合メモ";
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _matchTimer = new System.Timers.Timer(60000); // 1分間隔
            _matchTimer.Elapsed += OnTimerElapsed;
        }

        private async void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CurrentMatch != null && IsRealTimeMode)
            {
                CurrentMatch.CurrentMinute++;
                UpdateMatchTimeDisplay();

                // データベースに現在時刻を保存
                await _databaseService.SaveMatchAsync(CurrentMatch);
            }
        }

        [RelayCommand]
        async Task LoadMatchDataAsync()
        {
            if (IsBusy || MatchId == 0) return;

            try
            {
                IsBusy = true;

                // 試合情報を読み込み
                CurrentMatch = await _databaseService.GetMatchAsync(MatchId);
                if (CurrentMatch == null) return;

                IsRealTimeMode = CurrentMatch.IsRealTimeMode;
                UpdateMatchTimeDisplay();

                // フィールド上の選手を読み込み
                await LoadFieldPlayersAsync();

                // 両チームの選手を読み込み（既存機能との互換性のため）
                await LoadTeamPlayersAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"データの読み込みに失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFieldPlayersAsync()
        {
            var homeMatchPlayers = await _databaseService.GetHomeTeamPlayersAsync(MatchId);
            var awayMatchPlayers = await _databaseService.GetAwayTeamPlayersAsync(MatchId);
            var allPlayers = await _databaseService.GetPlayersAsync();

            // ホームチーム選手をフィールドに配置（下半分）
            HomeTeamPlayersOnField.Clear();
            foreach (var matchPlayer in homeMatchPlayers)
            {
                var player = allPlayers.FirstOrDefault(p => p.Id == matchPlayer.PlayerId);
                if (player != null)
                {
                    var fieldPlayer = new FieldPlayerView
                    {
                        Player = player,
                        X = matchPlayer.FieldX,
                        Y = AdjustPositionForHomeTeam(matchPlayer.FieldY), // 下半分に配置
                        IsHomeTeam = true
                    };
                    HomeTeamPlayersOnField.Add(fieldPlayer);
                }
            }

            // アウェイチーム選手をフィールドに配置（上半分）
            AwayTeamPlayersOnField.Clear();
            foreach (var matchPlayer in awayMatchPlayers)
            {
                var player = allPlayers.FirstOrDefault(p => p.Id == matchPlayer.PlayerId);
                if (player != null)
                {
                    var fieldPlayer = new FieldPlayerView
                    {
                        Player = player,
                        X = matchPlayer.FieldX,
                        Y = AdjustPositionForAwayTeam(matchPlayer.FieldY), // 上半分に配置
                        IsHomeTeam = false
                    };
                    AwayTeamPlayersOnField.Add(fieldPlayer);
                }
            }
        }

        /// <summary>
        /// ホームチームの位置を下半分（50-95%）に調整
        /// </summary>
        private double AdjustPositionForHomeTeam(double originalY)
        {
            // 元の位置（0-100%）を下半分（50-95%）にマッピング
            // GKエリア（0-10%）→（50-55%）
            // フィールドプレーヤー（10-90%）→（55-90%）
            // 最前線（90-100%）→（90-95%）

            if (originalY <= 10) // GK エリア
            {
                return 50 + (originalY * 0.5); // 50-55%
            }
            else if (originalY <= 90) // フィールドプレーヤー
            {
                return 55 + ((originalY - 10) * 0.4375); // 55-90%
            }
            else // 最前線
            {
                return 90 + ((originalY - 90) * 0.5); // 90-95%
            }
        }

        /// <summary>
        /// アウェイチームの位置を上半分（5-50%）に調整
        /// </summary>
        private double AdjustPositionForAwayTeam(double originalY)
        {
            // 元の位置（0-100%）を上半分（5-50%）にマッピング（上下反転）
            // 最前線（90-100%）→（5-10%）
            // フィールドプレーヤー（10-90%）→（10-45%）
            // GKエリア（0-10%）→（45-50%）

            if (originalY >= 90) // 最前線 → 上端
            {
                return 5 + ((100 - originalY) * 0.5); // 5-10%
            }
            else if (originalY >= 10) // フィールドプレーヤー → 中央上部
            {
                return 10 + ((90 - originalY) * 0.4375); // 10-45%
            }
            else // GK → 中央付近
            {
                return 45 + ((10 - originalY) * 0.5); // 45-50%
            }
        }

        private async Task LoadTeamPlayersAsync()
        {
            var homeMatchPlayers = await _databaseService.GetHomeTeamPlayersAsync(MatchId);
            var awayMatchPlayers = await _databaseService.GetAwayTeamPlayersAsync(MatchId);
            var allPlayers = await _databaseService.GetPlayersAsync();

            // ホームチーム選手を読み込み
            HomeTeamPlayers.Clear();
            foreach (var matchPlayer in homeMatchPlayers)
            {
                var player = allPlayers.FirstOrDefault(p => p.Id == matchPlayer.PlayerId);
                if (player != null)
                {
                    var memos = await _databaseService.GetMemosForPlayerAsync(MatchId, player.Id);
                    var playerWithMemos = new PlayerWithMemos
                    {
                        Player = player,
                        Memos = new ObservableCollection<Memo>(memos),
                        MemoCount = memos.Count
                    };
                    HomeTeamPlayers.Add(playerWithMemos);
                }
            }

            // アウェイチーム選手を読み込み
            AwayTeamPlayers.Clear();
            foreach (var matchPlayer in awayMatchPlayers)
            {
                var player = allPlayers.FirstOrDefault(p => p.Id == matchPlayer.PlayerId);
                if (player != null)
                {
                    var memos = await _databaseService.GetMemosForPlayerAsync(MatchId, player.Id);
                    var playerWithMemos = new PlayerWithMemos
                    {
                        Player = player,
                        Memos = new ObservableCollection<Memo>(memos),
                        MemoCount = memos.Count
                    };
                    AwayTeamPlayers.Add(playerWithMemos);
                }
            }
        }

        [RelayCommand]
        void SwitchTeam(object parameter)
        {
            bool isHome = true;
            if (parameter != null)
            {
                if (parameter is bool b)
                {
                    isHome = b;
                }
                else
                {
                    bool.TryParse(parameter.ToString(), out isHome);
                }
            }

            IsHomeTeamSelected = isHome;

            // チーム切り替え時に選手選択をクリア
            SelectedPlayer = null;
            CurrentPlayerMemos.Clear();
        }

        [RelayCommand]
        async Task SelectPlayerFromFieldAsync(Player player)
        {
            if (player == null) return;

            try
            {
                SelectedPlayer = player;

                // 選択した選手がどちらのチームかを判定
                var isPlayerInHomeTeam = HomeTeamPlayersOnField.Any(p => p.Player.Id == player.Id);
                IsHomeTeamSelected = isPlayerInHomeTeam;

                await LoadPlayerMemosAsync();

                // デバッグ用ログ
                System.Diagnostics.Debug.WriteLine($"Player selected: {player.Name}");

                // メイン UIスレッドでエディターを表示
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ShowMemoEditor();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting player: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task SelectPlayerAsync(Player player)
        {
            if (player == null) return;

            SelectedPlayer = player;

            // 選択した選手がどちらのチームかを判定
            var isPlayerInHomeTeam = HomeTeamPlayers.Any(p => p.Player.Id == player.Id);
            IsHomeTeamSelected = isPlayerInHomeTeam;

            await LoadPlayerMemosAsync();
        }

        [RelayCommand]
        async Task LoadPlayerMemosAsync()
        {
            if (SelectedPlayer == null) return;

            var memos = await _databaseService.GetMemosForPlayerAsync(MatchId, SelectedPlayer.Id);
            CurrentPlayerMemos.Clear();
            foreach (var memo in memos.OrderBy(m => m.MatchMinute))
            {
                CurrentPlayerMemos.Add(memo);
            }
        }

        [RelayCommand]
        async Task QuickMemoAsync(string templateText)
        {
            if (string.IsNullOrWhiteSpace(templateText) || SelectedPlayer == null)
            {
                await Shell.Current.DisplayAlert("エラー", "メモ内容と選手を選択してください", "OK");
                return;
            }

            try
            {
                var memo = new Memo
                {
                    MatchId = MatchId,
                    PlayerId = SelectedPlayer.Id,
                    Content = templateText,
                    MatchMinute = IsRealTimeMode ? CurrentMatch.CurrentMinute : 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _databaseService.SaveMemoAsync(memo);

                // UI更新
                CurrentPlayerMemos.Add(memo);

                // 選手のメモ数を更新
                await UpdatePlayerMemoCount(SelectedPlayer.Id);

                await Shell.Current.DisplayAlert("成功", "クイックメモを保存しました", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"メモの保存に失敗しました: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        async Task SaveMemoAsync()
        {
            if (string.IsNullOrWhiteSpace(MemoText) || SelectedPlayer == null)
            {
                await Shell.Current.DisplayAlert("エラー", "メモ内容と選手を選択してください", "OK");
                return;
            }

            try
            {
                var memo = new Memo
                {
                    MatchId = MatchId,
                    PlayerId = SelectedPlayer.Id,
                    Content = MemoText,
                    MatchMinute = IsRealTimeMode ? CurrentMatch.CurrentMinute : 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _databaseService.SaveMemoAsync(memo);

                // UI更新
                CurrentPlayerMemos.Add(memo);

                // 選手のメモ数を更新
                await UpdatePlayerMemoCount(SelectedPlayer.Id);

                // メモをクリア
                MemoText = string.Empty;

                // エディターを閉じる
                HideMemoEditor();

                await Shell.Current.DisplayAlert("成功", "メモを保存しました", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"メモの保存に失敗しました: {ex.Message}", "OK");
            }
        }

        private async Task UpdatePlayerMemoCount(int playerId)
        {
            var allPlayers = HomeTeamPlayers.Concat(AwayTeamPlayers);
            var playerWithMemos = allPlayers.FirstOrDefault(p => p.Player.Id == playerId);
            if (playerWithMemos != null)
            {
                var memos = await _databaseService.GetMemosForPlayerAsync(MatchId, playerId);
                playerWithMemos.Memos.Clear();
                foreach (var memo in memos)
                {
                    playerWithMemos.Memos.Add(memo);
                }
                playerWithMemos.MemoCount = memos.Count;
            }
        }

        [RelayCommand]
        void ShowMemoEditor()
        {
            IsEditorVisible = true;
        }

        [RelayCommand]
        void HideMemoEditor()
        {
            IsEditorVisible = false;
            IsEditorFocused = false;
            MemoText = string.Empty;
        }

        [RelayCommand]
        void CancelMemoEdit()
        {
            HideMemoEditor();
        }

        [RelayCommand]
        void AddTemplate(string template)
        {
            if (!string.IsNullOrWhiteSpace(template))
            {
                if (!string.IsNullOrWhiteSpace(MemoText))
                    MemoText += " ";
                MemoText += template;
            }
        }

        [RelayCommand]
        void StartStopTimer()
        {
            if (!IsRealTimeMode) return;

            if (IsTimerRunning)
            {
                _matchTimer.Stop();
                CurrentMatch.IsTimerPaused = true;
                IsTimerRunning = false;
            }
            else
            {
                _matchTimer.Start();
                CurrentMatch.IsTimerPaused = false;
                IsTimerRunning = true;
            }
        }

        [RelayCommand]
        void ResetTimer()
        {
            if (!IsRealTimeMode) return;

            _matchTimer.Stop();
            CurrentMatch.CurrentMinute = 0;
            CurrentMatch.IsTimerPaused = true;
            IsTimerRunning = false;
            UpdateMatchTimeDisplay();
        }

        [RelayCommand]
        void AddMinute()
        {
            if (CurrentMatch != null)
            {
                CurrentMatch.CurrentMinute++;
                UpdateMatchTimeDisplay();
            }
        }

        [RelayCommand]
        void SubtractMinute()
        {
            if (CurrentMatch != null && CurrentMatch.CurrentMinute > 0)
            {
                CurrentMatch.CurrentMinute--;
                UpdateMatchTimeDisplay();
            }
        }

        private void UpdateMatchTimeDisplay()
        {
            if (CurrentMatch != null)
            {
                var minutes = CurrentMatch.CurrentMinute;
                MatchTimeDisplay = $"{minutes:00}:00";
            }
        }

        // エディターフォーカス関連のメソッド（コードビハインドから呼び出される）
        public void OnMemoEditorFocused()
        {
            IsEditorFocused = true;
        }

        public void OnMemoEditorUnfocused()
        {
            IsEditorFocused = false;
        }

        public async Task OnAppearing()
        {
            await LoadMatchDataAsync();
        }

        public void OnDisappearing()
        {
            _matchTimer?.Stop();
            if (CurrentMatch != null)
            {
                // 現在の状態を保存
                Task.Run(async () => await _databaseService.SaveMatchAsync(CurrentMatch));
            }
        }
    }

    // フィールド表示用のヘルパークラス
    public partial class FieldPlayerView : ObservableObject
    {
        public Player Player { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsHomeTeam { get; set; }
    }

    // 既存のヘルパークラス
    public partial class PlayerWithMemos : ObservableObject
    {
        public Player Player { get; set; }
        public ObservableCollection<Memo> Memos { get; set; } = new();

        [ObservableProperty]
        int memoCount;
    }
}