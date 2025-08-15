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

        // 選手とメモのコレクション
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

                // 両チームの選手を読み込み
                await LoadTeamPlayersAsync();

                // 最初の選手を選択（ホームチームから）
                if (HomeTeamPlayers.Any())
                {
                    SelectedPlayer = HomeTeamPlayers.First().Player;
                    IsHomeTeamSelected = true;
                    await LoadPlayerMemosAsync();
                }
                else if (AwayTeamPlayers.Any())
                {
                    SelectedPlayer = AwayTeamPlayers.First().Player;
                    IsHomeTeamSelected = false;
                    await LoadPlayerMemosAsync();
                }
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
                var allPlayers = HomeTeamPlayers.Concat(AwayTeamPlayers);
                var playerWithMemos = allPlayers.FirstOrDefault(p => p.Player.Id == SelectedPlayer.Id);
                if (playerWithMemos != null)
                {
                    playerWithMemos.Memos.Add(memo);
                    playerWithMemos.MemoCount = playerWithMemos.Memos.Count;
                }

                // メモをクリア
                MemoText = string.Empty;

                await Shell.Current.DisplayAlert("成功", "メモを保存しました", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"メモの保存に失敗しました: {ex.Message}", "OK");
            }
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

    // ヘルパークラス
    public partial class PlayerWithMemos : ObservableObject
    {
        public Player Player { get; set; }
        public ObservableCollection<Memo> Memos { get; set; } = new();

        [ObservableProperty]
        int memoCount;
    }
}