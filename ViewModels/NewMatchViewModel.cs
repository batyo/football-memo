using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using System.Collections.ObjectModel;

namespace MatchMemoApp.ViewModels
{
    public partial class NewMatchViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        // 試合情報
        [ObservableProperty]
        DateTime matchDate = DateTime.Today;

        [ObservableProperty]
        string weather = "晴れ";

        [ObservableProperty]
        string opponent = string.Empty;

        [ObservableProperty]
        string stadium = string.Empty;

        [ObservableProperty]
        string selectedFormation = "4-4-2";

        [ObservableProperty]
        bool isRealTimeMode = true;

        // 選手管理
        public ObservableCollection<Player> AvailablePlayers { get; } = new();
        public ObservableCollection<FieldPlayer> FieldPlayers { get; } = new();

        // 天気オプション
        public List<string> WeatherOptions { get; } = new()
        {
            "晴れ", "曇り", "雨", "雪", "霧"
        };

        // フォーメーションオプション
        public List<string> FormationOptions { get; } = new()
        {
            "4-4-2", "4-3-3", "3-5-2", "4-2-3-1", "3-4-3", "5-3-2"
        };

        public NewMatchViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "新規試合作成";
        }

        [RelayCommand]
        async Task LoadPlayersAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var players = await _databaseService.GetPlayersAsync();

                AvailablePlayers.Clear();
                foreach (var player in players)
                {
                    AvailablePlayers.Add(player);
                }

                // デフォルト選手がない場合、サンプル選手を作成
                if (!players.Any())
                {
                    await CreateSamplePlayersAsync();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"選手データの読み込みに失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        void SetFormation(string formation)
        {
            SelectedFormation = formation;
            SetupFormation();
        }

        [RelayCommand]
        void AddPlayerToField(Player player)
        {
            if (FieldPlayers.Count >= 11)
            {
                Shell.Current.DisplayAlert("警告", "フィールドには最大11人まで配置できます", "OK");
                return;
            }

            // 既に配置済みかチェック
            if (FieldPlayers.Any(fp => fp.Player.Id == player.Id))
            {
                Shell.Current.DisplayAlert("警告", "この選手は既に配置されています", "OK");
                return;
            }

            // デフォルト位置を設定
            var defaultPosition = GetDefaultPosition(FieldPlayers.Count);
            var fieldPlayer = new FieldPlayer
            {
                Player = player,
                X = defaultPosition.X,
                Y = defaultPosition.Y,
                IsStarting = true
            };

            FieldPlayers.Add(fieldPlayer);
        }

        [RelayCommand]
        void RemovePlayerFromField(FieldPlayer fieldPlayer)
        {
            FieldPlayers.Remove(fieldPlayer);
        }

        [RelayCommand]
        async Task SaveMatchAsync()
        {
            if (string.IsNullOrWhiteSpace(Opponent))
            {
                await Shell.Current.DisplayAlert("エラー", "対戦相手を入力してください", "OK");
                return;
            }

            if (FieldPlayers.Count == 0)
            {
                await Shell.Current.DisplayAlert("エラー", "少なくとも1人の選手を配置してください", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // 試合を保存
                var match = new Match
                {
                    Date = MatchDate,
                    Weather = Weather,
                    Opponent = Opponent,
                    Stadium = Stadium,
                    Formation = SelectedFormation,
                    IsRealTimeMode = IsRealTimeMode,
                    CurrentMinute = 0,
                    IsTimerPaused = true
                };

                await _databaseService.SaveMatchAsync(match);

                // 最新の試合IDを取得
                var matches = await _databaseService.GetMatchesAsync();
                var savedMatch = matches.First();

                // 選手配置を保存
                foreach (var fieldPlayer in FieldPlayers)
                {
                    var matchPlayer = new MatchPlayer
                    {
                        MatchId = savedMatch.Id,
                        PlayerId = fieldPlayer.Player.Id,
                        FieldX = fieldPlayer.X,
                        FieldY = fieldPlayer.Y,
                        IsStarting = fieldPlayer.IsStarting
                    };

                    await _databaseService.SaveMatchPlayerAsync(matchPlayer);
                }

                await Shell.Current.DisplayAlert("成功", "試合が作成されました", "OK");
                await Shell.Current.GoToAsync($"//MainPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"試合の保存に失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SetupFormation()
        {
            FieldPlayers.Clear();

            // フォーメーションに基づいてデフォルト位置を設定
            var positions = GetFormationPositions(SelectedFormation);

            for (int i = 0; i < Math.Min(positions.Count, AvailablePlayers.Count); i++)
            {
                var fieldPlayer = new FieldPlayer
                {
                    Player = AvailablePlayers[i],
                    X = positions[i].X,
                    Y = positions[i].Y,
                    IsStarting = true
                };
                FieldPlayers.Add(fieldPlayer);
            }
        }

        private List<FieldPosition> GetFormationPositions(string formation)
        {
            return formation switch
            {
                "4-4-2" => new List<FieldPosition>
                {
                    new(50, 90),   // GK
                    new(20, 70), new(40, 70), new(60, 70), new(80, 70), // DF
                    new(20, 50), new(40, 50), new(60, 50), new(80, 50), // MF
                    new(35, 30), new(65, 30)  // FW
                },
                "4-3-3" => new List<FieldPosition>
                {
                    new(50, 90),   // GK
                    new(20, 70), new(40, 70), new(60, 70), new(80, 70), // DF
                    new(30, 50), new(50, 50), new(70, 50), // MF
                    new(25, 30), new(50, 30), new(75, 30)  // FW
                },
                "3-5-2" => new List<FieldPosition>
                {
                    new(50, 90),   // GK
                    new(30, 70), new(50, 70), new(70, 70), // DF
                    new(20, 50), new(35, 50), new(50, 50), new(65, 50), new(80, 50), // MF
                    new(40, 30), new(60, 30)  // FW
                },
                _ => new List<FieldPosition>
                {
                    new(50, 90), new(20, 70), new(40, 70), new(60, 70), new(80, 70),
                    new(20, 50), new(40, 50), new(60, 50), new(80, 50),
                    new(35, 30), new(65, 30)
                }
            };
        }

        private FieldPosition GetDefaultPosition(int playerCount)
        {
            var defaultPositions = GetFormationPositions(SelectedFormation);
            return playerCount < defaultPositions.Count
                ? defaultPositions[playerCount]
                : new FieldPosition(50, 50);
        }

        private async Task CreateSamplePlayersAsync()
        {
            var samplePlayers = new List<Player>
            {
                new() { Name = "田中太郎", Position = "GK", Number = 1, PreferredFoot = "右足" },
                new() { Name = "佐藤次郎", Position = "DF", Number = 2, PreferredFoot = "右足" },
                new() { Name = "鈴木三郎", Position = "DF", Number = 3, PreferredFoot = "左足" },
                new() { Name = "高橋四郎", Position = "DF", Number = 4, PreferredFoot = "右足" },
                new() { Name = "伊藤五郎", Position = "DF", Number = 5, PreferredFoot = "左足" },
                new() { Name = "山田六郎", Position = "MF", Number = 6, PreferredFoot = "右足" },
                new() { Name = "中村七郎", Position = "MF", Number = 7, PreferredFoot = "左足" },
                new() { Name = "小林八郎", Position = "MF", Number = 8, PreferredFoot = "右足" },
                new() { Name = "加藤九郎", Position = "MF", Number = 9, PreferredFoot = "両足" },
                new() { Name = "吉田十郎", Position = "FW", Number = 10, PreferredFoot = "右足" },
                new() { Name = "松本十一", Position = "FW", Number = 11, PreferredFoot = "左足" }
            };

            foreach (var player in samplePlayers)
            {
                await _databaseService.SavePlayerAsync(player);
                AvailablePlayers.Add(player);
            }
        }

        public async Task OnAppearing()
        {
            await LoadPlayersAsync();
        }
    }

    // ヘルパークラス
    public partial class FieldPlayer : ObservableObject
    {
        public Player Player { get; set; }

        [ObservableProperty]
        double x;

        [ObservableProperty]
        double y;

        [ObservableProperty]
        bool isStarting;
    }

    public record FieldPosition(double X, double Y);
}