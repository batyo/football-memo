using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using MatchMemoApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        // 選手エディター関連
        [ObservableProperty]
        bool isPlayerEditorVisible = false;

        [ObservableProperty]
        string playerEditorTitle = "新規選手登録";

        [ObservableProperty]
        string currentPlayerName = string.Empty;

        [ObservableProperty]
        string currentPlayerNumber = string.Empty;

        [ObservableProperty]
        string currentPlayerPreferredFoot = "右足";

        [ObservableProperty]
        string currentPlayerHeight = string.Empty;

        [ObservableProperty]
        bool isEditingExistingPlayer = false;

        // フォーメーション配置された選手
        public ObservableCollection<FormationPlayer> HomeTeamPlayers { get; } = new();
        public ObservableCollection<FormationPlayer> AwayTeamPlayers { get; } = new();

        // データベース選手（中優先度）
        public ObservableCollection<Player> DatabasePlayers { get; } = new();

        // 選択肢
        public List<string> WeatherOptions { get; } = new()
        {
            "晴れ", "曇り", "雨", "雪", "霧"
        };

        public List<string> FormationOptions { get; } = new()
        {
            "4-4-2", "4-3-3", "3-5-2", "4-2-3-1", "3-4-3", "5-3-2", "4-5-1"
        };

        public List<string> PreferredFootOptions { get; } = new()
        {
            "右足", "左足", "両足"
        };

        // 現在編集中の選手とポジション
        private FormationPlayer? _currentEditingPlayer;
        private bool _currentEditingIsHome;

        public NewMatchViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "新規試合作成";

            // フォーメーション変更時の監視
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HomeTeamFormation))
                    SetupFormation(isHome: true);
                if (e.PropertyName == nameof(AwayTeamFormation))
                    SetupFormation(isHome: false);
            };

            // 初期フォーメーション設定
            SetupFormation(isHome: true);
            SetupFormation(isHome: false);
        }

        /// <summary>
        /// フォーメーションに基づいて選手配置を設定
        /// </summary>
        private void SetupFormation(bool isHome)
        {
            var formation = isHome ? HomeTeamFormation : AwayTeamFormation;
            var players = isHome ? HomeTeamPlayers : AwayTeamPlayers;

            // 既存の配置された選手の情報を保持
            var existingPlayers = players.ToList();
            players.Clear();

            var positions = GetFormationPositions(formation, isHome);

            for (int i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                FormationPlayer player;

                // 既存の選手がいれば再利用、いなければ新規作成
                if (i < existingPlayers.Count)
                {
                    player = existingPlayers[i];
                    player.X = position.X;
                    player.Y = position.Y;
                }
                else
                {
                    player = new FormationPlayer
                    {
                        X = position.X,
                        Y = position.Y,
                        IsHomeTeam = isHome,
                        IsConfigured = false
                    };
                }

                players.Add(player);
            }
        }

        /// <summary>
        /// フォーメーションごとの選手位置を取得
        /// </summary>
        private List<FieldPosition> GetFormationPositions(string formation, bool isHome)
        {
            var basePositions = formation switch
            {
                "4-4-2" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(20, 30), new(40, 30), new(60, 30), new(80, 30), // DF
                    new(20, 55), new(40, 55), new(60, 55), new(80, 55), // MF
                    new(35, 80), new(65, 80)  // FW
                },
                "4-3-3" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(20, 30), new(40, 30), new(60, 30), new(80, 30), // DF
                    new(30, 55), new(50, 55), new(70, 55), // MF
                    new(25, 80), new(50, 80), new(75, 80)  // FW
                },
                "3-5-2" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(30, 30), new(50, 30), new(70, 30), // DF
                    new(20, 50), new(35, 55), new(50, 55), new(65, 55), new(80, 50), // MF
                    new(40, 80), new(60, 80)  // FW
                },
                "4-2-3-1" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(20, 30), new(40, 30), new(60, 30), new(80, 30), // DF
                    new(35, 50), new(65, 50), // DMF
                    new(25, 70), new(50, 70), new(75, 70), // AMF
                    new(50, 85)  // FW
                },
                "3-4-3" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(30, 30), new(50, 30), new(70, 30), // DF
                    new(25, 55), new(45, 55), new(55, 55), new(75, 55), // MF
                    new(25, 80), new(50, 80), new(75, 80)  // FW
                },
                "5-3-2" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(15, 30), new(30, 30), new(50, 30), new(70, 30), new(85, 30), // DF
                    new(30, 55), new(50, 55), new(70, 55), // MF
                    new(40, 80), new(60, 80)  // FW
                },
                "4-5-1" => new List<FieldPosition>
                {
                    new(50, 10),   // GK
                    new(20, 30), new(40, 30), new(60, 30), new(80, 30), // DF
                    new(20, 50), new(35, 55), new(50, 55), new(65, 55), new(80, 50), // MF
                    new(50, 80)  // FW
                },
                _ => new List<FieldPosition> // デフォルト4-4-2
                {
                    new(50, 10), new(20, 30), new(40, 30), new(60, 30), new(80, 30),
                    new(20, 55), new(40, 55), new(60, 55), new(80, 55),
                    new(35, 80), new(65, 80)
                }
            };

            // アウェイチーム（上半分）の場合は上下反転
            if (!isHome)
            {
                basePositions = basePositions.Select(p => new FieldPosition(p.X, 100 - p.Y)).ToList();
            }

            return basePositions;
        }

        /// <summary>
        /// 選手アイコンがタップされた時の処理
        /// </summary>
        [RelayCommand]
        void EditPlayer(object parameter)
        {
            if (parameter is not FormationPlayer player) return;

            _currentEditingPlayer = player;
            _currentEditingIsHome = player.IsHomeTeam;

            // エディターにプレイヤー情報をセット
            if (player.IsConfigured)
            {
                // 既存選手の編集
                PlayerEditorTitle = "選手情報編集";
                IsEditingExistingPlayer = true;
                CurrentPlayerName = player.Name ?? "";
                CurrentPlayerNumber = player.Number?.ToString() ?? "";
                CurrentPlayerPreferredFoot = player.PreferredFoot ?? "右足";
                CurrentPlayerHeight = player.Height?.ToString() ?? "";
            }
            else
            {
                // 新規選手の登録
                PlayerEditorTitle = "新規選手登録";
                IsEditingExistingPlayer = false;
                CurrentPlayerName = "";
                CurrentPlayerNumber = "";
                CurrentPlayerPreferredFoot = "右足";
                CurrentPlayerHeight = "";
            }

            IsPlayerEditorVisible = true;
        }

        /// <summary>
        /// 選手情報を保存
        /// </summary>
        [RelayCommand]
        async Task SavePlayerInfo()
        {
            // 入力検証
            if (string.IsNullOrWhiteSpace(CurrentPlayerName))
            {
                await Shell.Current.DisplayAlert("エラー", "選手名を入力してください", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentPlayerNumber) || !int.TryParse(CurrentPlayerNumber, out int number))
            {
                await Shell.Current.DisplayAlert("エラー", "背番号を正しく入力してください", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentPlayerPreferredFoot))
            {
                await Shell.Current.DisplayAlert("エラー", "利き足を選択してください", "OK");
                return;
            }

            // 背番号の重複チェック
            var allPlayers = HomeTeamPlayers.Concat(AwayTeamPlayers);
            var duplicatePlayer = allPlayers.FirstOrDefault(p =>
                p != _currentEditingPlayer &&
                p.IsConfigured &&
                p.Number == number);

            if (duplicatePlayer != null)
            {
                await Shell.Current.DisplayAlert("エラー", "この背番号は既に使用されています", "OK");
                return;
            }

            // 選手情報を更新
            if (_currentEditingPlayer != null)
            {
                _currentEditingPlayer.Name = CurrentPlayerName;
                _currentEditingPlayer.Number = number;
                _currentEditingPlayer.PreferredFoot = CurrentPlayerPreferredFoot;
                _currentEditingPlayer.Height = int.TryParse(CurrentPlayerHeight, out int height) ? height : null;
                _currentEditingPlayer.IsConfigured = true;
            }

            IsPlayerEditorVisible = false;
            _currentEditingPlayer = null;
        }

        /// <summary>
        /// 選手情報編集をキャンセル
        /// </summary>
        [RelayCommand]
        void CancelPlayerEdit()
        {
            IsPlayerEditorVisible = false;
            _currentEditingPlayer = null;
        }

        /// <summary>
        /// データベース選手一覧表示（中優先度機能）
        /// </summary>
        [RelayCommand]
        async Task ShowPlayerDatabase()
        {
            try
            {
                IsBusy = true;
                var players = await _databaseService.GetPlayersAsync();

                DatabasePlayers.Clear();
                foreach (var player in players)
                {
                    DatabasePlayers.Add(player);
                }

                // データベース選手選択画面を表示（実装は次のステップ）
                await Shell.Current.DisplayAlert("情報", "データベース選手機能は次のバージョンで実装予定です", "OK");
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

        /// <summary>
        /// 試合を保存
        /// </summary>
        [RelayCommand]
        async Task SaveMatch()
        {
            if (string.IsNullOrWhiteSpace(HomeTeamName) || string.IsNullOrWhiteSpace(AwayTeamName))
            {
                await Shell.Current.DisplayAlert("エラー", "チーム名を入力してください", "OK");
                return;
            }

            var configuredHomePlayers = HomeTeamPlayers.Count(p => p.IsConfigured);
            var configuredAwayPlayers = AwayTeamPlayers.Count(p => p.IsConfigured);

            if (configuredHomePlayers == 0 && configuredAwayPlayers == 0)
            {
                await Shell.Current.DisplayAlert("エラー", "少なくとも1人の選手を設定してください", "OK");
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

                // 設定済み選手のみをデータベースに保存
                await SaveConfiguredPlayersToDatabase(savedMatch.Id);

                await Shell.Current.DisplayAlert("成功", "試合が作成されました", "OK");

                // 試合詳細画面に遷移
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

        /// <summary>
        /// 設定済み選手をデータベースに保存
        /// </summary>
        private async Task SaveConfiguredPlayersToDatabase(int matchId)
        {
            var allConfiguredPlayers = HomeTeamPlayers.Concat(AwayTeamPlayers)
                .Where(p => p.IsConfigured);

            foreach (var formationPlayer in allConfiguredPlayers)
            {
                // まず選手をPlayersテーブルに保存
                var player = new Player
                {
                    Name = formationPlayer.Name!,
                    Number = formationPlayer.Number!.Value,
                    Position = GetPositionByLocation(formationPlayer.X, formationPlayer.Y, formationPlayer.IsHomeTeam),
                    PreferredFoot = formationPlayer.PreferredFoot!,
                    ImagePath = formationPlayer.ImagePath
                };

                await _databaseService.SavePlayerAsync(player);

                // 保存後の選手IDを取得
                var savedPlayers = await _databaseService.GetPlayersAsync();
                var savedPlayer = savedPlayers.LastOrDefault(p =>
                    p.Name == player.Name &&
                    p.Number == player.Number);

                if (savedPlayer != null)
                {
                    // MatchPlayerテーブルに配置情報を保存
                    var matchPlayer = new MatchPlayer
                    {
                        MatchId = matchId,
                        PlayerId = savedPlayer.Id,
                        FieldX = formationPlayer.X,
                        FieldY = formationPlayer.Y,
                        IsStarting = true,
                        IsHomeTeam = formationPlayer.IsHomeTeam
                    };

                    await _databaseService.SaveMatchPlayerAsync(matchPlayer);
                }
            }
        }

        /// <summary>
        /// フィールド位置からポジションを推定
        /// </summary>
        private string GetPositionByLocation(double x, double y, bool isHome)
        {
            // 簡易的なポジション判定
            double adjustedY = isHome ? y : 100 - y;

            return adjustedY switch
            {
                <= 20 => "GK",
                <= 40 => "DF",
                <= 70 => "MF",
                _ => "FW"
            };
        }

        public async Task OnAppearing()
        {
            // 初期化処理があれば実行
        }
    }

    /// <summary>
    /// フォーメーション上の選手を表現するクラス
    /// </summary>
    public partial class FormationPlayer : ObservableObject
    {
        [ObservableProperty]
        double x;

        [ObservableProperty]
        double y;

        [ObservableProperty]
        bool isHomeTeam;

        [ObservableProperty]
        bool isConfigured;

        [ObservableProperty]
        string? name;

        [ObservableProperty]
        int? number;

        [ObservableProperty]
        string? preferredFoot;

        [ObservableProperty]
        int? height;

        [ObservableProperty]
        string? imagePath;

        /// <summary>
        /// 表示用の背番号テキスト
        /// </summary>
        public string DisplayNumber => Number?.ToString() ?? "?";

        /// <summary>
        /// 表示用の選手名（短縮）
        /// </summary>
        public string DisplayName => IsConfigured && !string.IsNullOrEmpty(Name)
            ? (Name.Length > 6 ? Name.Substring(0, 6) + "..." : Name)
            : "";

        /// <summary>
        /// チームカラー
        /// </summary>
        public Color TeamColor => IsHomeTeam
            ? Color.FromArgb("#2196F3")
            : Color.FromArgb("#F44336");
    }

    /// <summary>
    /// フィールド上の位置を表すレコード
    /// </summary>
    public record FieldPosition(double X, double Y);
}