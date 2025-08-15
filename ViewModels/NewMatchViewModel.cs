using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using MatchMemoApp.Views;
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
        string homeTeamName = "ホームチーム";

        [ObservableProperty]
        string awayTeamName = "アウェイチーム";

        [ObservableProperty]
        string stadium = string.Empty;

        [ObservableProperty]
        string homeTeamFormation = "4-4-2";

        [ObservableProperty]
        string awayTeamFormation = "4-4-2";

        [ObservableProperty]
        bool isRealTimeMode = true;

        [ObservableProperty]
        bool isHomeTeamSelected = true;

        // 選手管理
        public ObservableCollection<Player> AvailablePlayers { get; } = new();
        public ObservableCollection<FieldPlayer> HomeTeamPlayers { get; } = new();
        public ObservableCollection<FieldPlayer> AwayTeamPlayers { get; } = new();

        // 天気オプション
        public List<string> WeatherOptions { get; } = new()
        {
            "晴れ", "曇り", "雨", "雪", "霧"
        };

        // フォーメーションオプション
        public List<string> FormationOptions { get; } = new()
        {
            "4-4-2", "4-3-3", "3-5-2", "4-2-3-1", "3-4-3", "5-3-2", "4-5-1"
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
        void SetHomeTeamFormation(string formation)
        {
            HomeTeamFormation = formation;
            SetupFormation(true);
        }

        [RelayCommand]
        void SetAwayTeamFormation(string formation)
        {
            AwayTeamFormation = formation;
            SetupFormation(false);
        }

        [RelayCommand]
        void SwitchTeam(object parameter)
        {
            // CommandParameterはobject型で渡されるため、boolに変換
            bool isHome = true;
            if (parameter != null)
            {
                // 文字列やbool型どちらでも対応
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
        }

        [RelayCommand]
        void AddPlayerToField(Player player)
        {
            var currentTeamPlayers = IsHomeTeamSelected ? HomeTeamPlayers : AwayTeamPlayers;
            var maxPlayers = 11;

            if (currentTeamPlayers.Count >= maxPlayers)
            {
                Shell.Current.DisplayAlert("警告", "フィールドには最大11人まで配置できます", "OK");
                return;
            }

            // 既に配置済みかチェック
            if (currentTeamPlayers.Any(fp => fp.Player.Id == player.Id))
            {
                Shell.Current.DisplayAlert("警告", "この選手は既に配置されています", "OK");
                return;
            }

            // 他のチームに既に配置されているかチェック
            var otherTeamPlayers = IsHomeTeamSelected ? AwayTeamPlayers : HomeTeamPlayers;
            if (otherTeamPlayers.Any(fp => fp.Player.Id == player.Id))
            {
                Shell.Current.DisplayAlert("警告", "この選手は相手チームに既に配置されています", "OK");
                return;
            }

            // デフォルト位置を設定
            var formation = IsHomeTeamSelected ? HomeTeamFormation : AwayTeamFormation;
            var defaultPosition = GetDefaultPosition(currentTeamPlayers.Count, formation, IsHomeTeamSelected);

            var fieldPlayer = new FieldPlayer
            {
                Player = player,
                X = defaultPosition.X,
                Y = defaultPosition.Y,
                IsStarting = true,
                IsHomeTeam = IsHomeTeamSelected
            };

            currentTeamPlayers.Add(fieldPlayer);
        }

        [RelayCommand]
        void RemovePlayerFromField(FieldPlayer fieldPlayer)
        {
            if (fieldPlayer.IsHomeTeam)
                HomeTeamPlayers.Remove(fieldPlayer);
            else
                AwayTeamPlayers.Remove(fieldPlayer);
        }

        [RelayCommand]
        async Task SaveMatchAsync()
        {
            if (string.IsNullOrWhiteSpace(HomeTeamName) || string.IsNullOrWhiteSpace(AwayTeamName))
            {
                await Shell.Current.DisplayAlert("エラー", "チーム名を入力してください", "OK");
                return;
            }

            if (HomeTeamPlayers.Count == 0 && AwayTeamPlayers.Count == 0)
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
                    Opponent = AwayTeamName, // 後方互換のため
                    Stadium = Stadium,
                    Formation = HomeTeamFormation, // 後方互換のため
                    IsRealTimeMode = IsRealTimeMode,
                    CurrentMinute = 0,
                    IsTimerPaused = true,
                    HomeTeamName = HomeTeamName,
                    AwayTeamName = AwayTeamName,
                    HomeTeamFormation = HomeTeamFormation,
                    AwayTeamFormation = AwayTeamFormation
                };

                await _databaseService.SaveMatchAsync(match);

                // 最新の試合IDを取得
                var matches = await _databaseService.GetMatchesAsync();
                var savedMatch = matches.First();

                // 両チームの選手配置を保存
                foreach (var fieldPlayer in HomeTeamPlayers.Concat(AwayTeamPlayers))
                {
                    var matchPlayer = new MatchPlayer
                    {
                        MatchId = savedMatch.Id,
                        PlayerId = fieldPlayer.Player.Id,
                        FieldX = fieldPlayer.X,
                        FieldY = fieldPlayer.Y,
                        IsStarting = fieldPlayer.IsStarting,
                        IsHomeTeam = fieldPlayer.IsHomeTeam
                    };

                    await _databaseService.SaveMatchPlayerAsync(matchPlayer);
                }

                await Shell.Current.DisplayAlert("成功", "試合が作成されました", "OK");

                // 試合詳細画面に直接遷移
                await Shell.Current.GoToAsync($"{nameof(MatchDetailPage)}?MatchId={savedMatch.Id}");
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

        private void SetupFormation(bool isHomeTeam)
        {
            var currentTeamPlayers = isHomeTeam ? HomeTeamPlayers : AwayTeamPlayers;
            var formation = isHomeTeam ? HomeTeamFormation : AwayTeamFormation;

            // 既存の選手配置をクリア
            var playersToReposition = currentTeamPlayers.ToList();
            currentTeamPlayers.Clear();

            // フォーメーションに基づいて再配置
            var positions = GetFormationPositions(formation, isHomeTeam);

            for (int i = 0; i < Math.Min(positions.Count, playersToReposition.Count); i++)
            {
                var fieldPlayer = playersToReposition[i];
                fieldPlayer.X = positions[i].X;
                fieldPlayer.Y = positions[i].Y;
                currentTeamPlayers.Add(fieldPlayer);
            }
        }

        private List<FieldPosition> GetFormationPositions(string formation, bool isHomeTeam)
        {
            // 縦向きフィールド用の座標（0-100の範囲）
            // ホームチーム（下側）とアウェイチーム（上側）で上下反転
            var positions = formation switch
            {
                "4-4-2" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(20, 25), new(40, 25), new(60, 25), new(80, 25), // DF
                    new(20, 50), new(40, 50), new(60, 50), new(80, 50), // MF
                    new(35, 75), new(65, 75)  // FW
                },
                "4-3-3" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(20, 25), new(40, 25), new(60, 25), new(80, 25), // DF
                    new(30, 50), new(50, 50), new(70, 50), // MF
                    new(25, 75), new(50, 75), new(75, 75)  // FW
                },
                "3-5-2" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(30, 25), new(50, 25), new(70, 25), // DF
                    new(20, 50), new(35, 50), new(50, 50), new(65, 50), new(80, 50), // MF
                    new(40, 75), new(60, 75)  // FW
                },
                "4-2-3-1" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(20, 25), new(40, 25), new(60, 25), new(80, 25), // DF
                    new(35, 45), new(65, 45), // DMF
                    new(25, 65), new(50, 65), new(75, 65), // AMF
                    new(50, 80)  // FW
                },
                "3-4-3" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(30, 25), new(50, 25), new(70, 25), // DF
                    new(25, 50), new(45, 50), new(55, 50), new(75, 50), // MF
                    new(25, 75), new(50, 75), new(75, 75)  // FW
                },
                "5-3-2" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(15, 25), new(30, 25), new(50, 25), new(70, 25), new(85, 25), // DF
                    new(30, 50), new(50, 50), new(70, 50), // MF
                    new(40, 75), new(60, 75)  // FW
                },
                "4-5-1" => new List<FieldPosition>
                {
                    new(50, 5),   // GK
                    new(20, 25), new(40, 25), new(60, 25), new(80, 25), // DF
                    new(20, 45), new(35, 50), new(50, 50), new(65, 50), new(80, 45), // MF
                    new(50, 75)  // FW
                },
                _ => new List<FieldPosition>
                {
                    new(50, 5), new(20, 25), new(40, 25), new(60, 25), new(80, 25),
                    new(20, 50), new(40, 50), new(60, 50), new(80, 50),
                    new(35, 75), new(65, 75)
                }
            };

            // アウェイチーム（上側）の場合は上下反転
            if (!isHomeTeam)
            {
                positions = positions.Select(p => new FieldPosition(p.X, 100 - p.Y)).ToList();
            }

            return positions;
        }

        private FieldPosition GetDefaultPosition(int playerCount, string formation, bool isHomeTeam)
        {
            var defaultPositions = GetFormationPositions(formation, isHomeTeam);
            return playerCount < defaultPositions.Count
                ? defaultPositions[playerCount]
                : new FieldPosition(50, isHomeTeam ? 50 : 50);
        }

        private async Task CreateSamplePlayersAsync()
        {
            var samplePlayers = new List<Player>
            {
                // ホームチーム想定選手
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
                new() { Name = "松本十一", Position = "FW", Number = 11, PreferredFoot = "左足" },
                
                // 追加選手（相手チーム用）
                new() { Name = "渡辺十二", Position = "GK", Number = 12, PreferredFoot = "右足" },
                new() { Name = "斉藤十三", Position = "DF", Number = 13, PreferredFoot = "右足" },
                new() { Name = "森田十四", Position = "DF", Number = 14, PreferredFoot = "左足" },
                new() { Name = "池田十五", Position = "DF", Number = 15, PreferredFoot = "右足" },
                new() { Name = "橋本十六", Position = "DF", Number = 16, PreferredFoot = "左足" },
                new() { Name = "石川十七", Position = "MF", Number = 17, PreferredFoot = "右足" },
                new() { Name = "前田十八", Position = "MF", Number = 18, PreferredFoot = "左足" },
                new() { Name = "岡田十九", Position = "MF", Number = 19, PreferredFoot = "右足" },
                new() { Name = "長谷川二十", Position = "MF", Number = 20, PreferredFoot = "両足" },
                new() { Name = "清水二十一", Position = "FW", Number = 21, PreferredFoot = "右足" },
                new() { Name = "山本二十二", Position = "FW", Number = 22, PreferredFoot = "左足" }
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

        [ObservableProperty]
        bool isHomeTeam;
    }

    public record FieldPosition(double X, double Y);
}