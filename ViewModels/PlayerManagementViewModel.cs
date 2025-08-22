using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace MatchMemoApp.ViewModels
{
    public partial class PlayerManagementViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<Player> Players { get; } = new();

        [ObservableProperty]
        Player selectedPlayer;

        [ObservableProperty]
        bool isEditMode = false;

        [ObservableProperty]
        bool showEditor = false;

        [ObservableProperty]
        string playerName = string.Empty;

        [ObservableProperty]
        string playerPosition = "FW";

        [ObservableProperty]
        int playerNumber = 1;

        [ObservableProperty]
        string preferredFoot = "右足";

        [ObservableProperty]
        string playerImagePath = string.Empty;

        // デバッグ関連プロパティ
        [ObservableProperty]
        bool showDebugInfo = false;

        [ObservableProperty]
        string debugMessage = string.Empty;

        [ObservableProperty]
        string lastErrorDetails = string.Empty;

        // バリデーション関連プロパティ
        [ObservableProperty]
        bool hasValidationError = false;

        [ObservableProperty]
        string validationErrorMessage = string.Empty;

        public List<string> PositionOptions { get; } = new()
        {
            "GK", "DF", "MF", "FW"
        };

        public List<string> FootOptions { get; } = new()
        {
            "右足", "左足", "両足"
        };

        public PlayerManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "選手管理";

            // デバッグモードの判定（開発時のみ有効）
#if DEBUG
            ShowDebugInfo = true;
            DebugMessage = "デバッグモード: ON";
#endif
        }

        [RelayCommand]
        async Task LoadPlayersAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                LogDebug("選手データ読み込み開始");

                var players = await _databaseService.GetPlayersAsync();
                LogDebug($"データベースから{players.Count}人の選手を取得");

                // UIスレッドで確実に更新
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        LogDebug("UIスレッドでコレクション更新開始");

                        // 既存のコレクションをクリア
                        Players.Clear();
                        LogDebug("既存コレクションクリア完了");

                        // 少し待機してからデータを追加（Android CollectionViewの描画問題対応）
                        System.Threading.Tasks.Task.Delay(50).Wait();

                        // 番号順でソート
                        var sortedPlayers = players.OrderBy(p => p.Number).ToList();
                        LogDebug($"選手ソート完了: {sortedPlayers.Count}人");

                        // 一つずつ追加してログ出力
                        foreach (var player in sortedPlayers)
                        {
                            // nullチェックと値の補完
                            if (string.IsNullOrEmpty(player.Name))
                                player.Name = "名前未設定";
                            if (string.IsNullOrEmpty(player.Position))
                                player.Position = "FW";
                            if (string.IsNullOrEmpty(player.PreferredFoot))
                                player.PreferredFoot = "右足";

                            Players.Add(player);
                            LogDebug($"選手追加: {player.Name} (#{player.Number})");
                        }

                        LogDebug($"コレクション更新完了: Players.Count = {Players.Count}");

                        // コレクション変更の強制通知（複数回実行で確実に）
                        OnPropertyChanged(nameof(Players));

                        // さらに遅延実行での再通知（CollectionView描画問題の回避）
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(100);
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                OnPropertyChanged(nameof(Players));
                                LogDebug("遅延通知実行");
                            });
                        });

                        LogDebug("コレクション変更通知送信");
                    }
                    catch (Exception uiEx)
                    {
                        LogError("UIスレッドでのコレクション更新に失敗", uiEx);
                        throw;
                    }
                });

                LogDebug($"選手データ読み込み完了: {players.Count}人");
            }
            catch (Exception ex)
            {
                LogError("選手データの読み込みに失敗", ex);
                await ShowErrorAlert("選手データの読み込みに失敗しました", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task NewPlayer()
        {
            try
            {
                LogDebug("新規選手登録画面を開始");
                ClearForm();
                ClearValidationErrors();
                IsEditMode = false;
                SelectedPlayer = null;
                ShowEditor = true;
            }
            catch (Exception ex)
            {
                LogError("新規選手登録画面の表示に失敗", ex);
                await ShowErrorAlert("画面表示エラー", ex.Message);
            }
        }

        [RelayCommand]
        async Task EditPlayer(Player player)
        {
            if (player == null)
            {
                LogError("EditPlayer: playerがnull");
                return;
            }

            try
            {
                LogDebug($"選手編集開始: {player.Name} (ID: {player.Id})");

                SelectedPlayer = player;
                PlayerName = player.Name ?? string.Empty;
                PlayerPosition = player.Position ?? "FW";
                PlayerNumber = player.Number;
                PreferredFoot = player.PreferredFoot ?? "右足";
                PlayerImagePath = player.ImagePath ?? string.Empty;

                ClearValidationErrors();
                IsEditMode = true;
                ShowEditor = true;

                LogDebug("選手編集画面表示完了");
            }
            catch (Exception ex)
            {
                LogError($"選手編集画面の表示に失敗 (Player: {player?.Name})", ex);
                await ShowErrorAlert("編集画面表示エラー", ex.Message);
            }
        }

        [RelayCommand]
        async Task SavePlayer()
        {
            try
            {
                LogDebug($"選手保存開始: {PlayerName} ({(IsEditMode ? "編集" : "新規")})");

                // バリデーション
                if (!ValidatePlayerData())
                {
                    LogDebug("バリデーションエラーのため保存を中断");
                    return;
                }

                IsBusy = true;

                Player player;
                if (IsEditMode && SelectedPlayer != null)
                {
                    // 編集モード
                    player = SelectedPlayer;
                    player.Name = PlayerName.Trim();
                    player.Position = PlayerPosition;
                    player.Number = PlayerNumber;
                    player.PreferredFoot = PreferredFoot;
                    player.ImagePath = PlayerImagePath;

                    LogDebug($"既存選手更新: ID={player.Id}");
                }
                else
                {
                    // 新規作成モード
                    player = new Player
                    {
                        Name = PlayerName.Trim(),
                        Position = PlayerPosition,
                        Number = PlayerNumber,
                        PreferredFoot = PreferredFoot,
                        ImagePath = PlayerImagePath
                    };

                    LogDebug("新規選手作成");
                }

                await _databaseService.SavePlayerAsync(player);
                LogDebug($"データベース保存完了: {player.Name}");

                // UI更新
                await UpdatePlayersCollection(player);

                // エディターを閉じる
                ShowEditor = false;
                ClearForm();

                await ShowSuccessAlert(
                    "保存完了",
                    $"選手情報を{(IsEditMode ? "更新" : "保存")}しました");

                LogDebug("選手保存処理完了");
            }
            catch (Exception ex)
            {
                LogError($"選手保存処理に失敗 (Name: {PlayerName})", ex);
                await ShowErrorAlert("保存エラー", $"選手情報の保存に失敗しました。\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task DeletePlayer(Player player)
        {
            if (player == null)
            {
                LogError("DeletePlayer: playerがnull");
                return;
            }

            try
            {
                LogDebug($"選手削除確認: {player.Name} (ID: {player.Id})");

                bool result = await Shell.Current.DisplayAlert(
                    "選手削除の確認",
                    $"「{player.Name}」を削除しますか？\n\n※この操作は取り消せません。",
                    "削除する",
                    "キャンセル");

                if (!result)
                {
                    LogDebug("選手削除をキャンセル");
                    return;
                }

                IsBusy = true;
                LogDebug($"選手削除実行開始: {player.Name}");

                // データベースから削除
                await _databaseService.DeletePlayerAsync(player.Id);
                LogDebug("データベースから削除完了");

                // UI更新
                Players.Remove(player);

                // 編集中だった場合はフォームをクリア
                if (SelectedPlayer?.Id == player.Id)
                {
                    ShowEditor = false;
                    ClearForm();
                    LogDebug("編集フォームをクリア");
                }

                await ShowSuccessAlert("削除完了", $"「{player.Name}」を削除しました");
                LogDebug("選手削除処理完了");
            }
            catch (Exception ex)
            {
                LogError($"選手削除処理に失敗 (Player: {player?.Name})", ex);
                await ShowErrorAlert("削除エラー", $"選手の削除に失敗しました。\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task SelectImage()
        {
            try
            {
                LogDebug("画像選択開始");

                var result = await MediaPicker.Default.PickPhotoAsync();
                if (result != null)
                {
                    LogDebug($"画像選択完了: {result.FileName}");

                    // アプリのローカルフォルダにファイルをコピー
                    var fileName = $"player_{DateTime.Now.Ticks}.jpg";
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, "PlayerImages", fileName);

                    // ディレクトリが存在しない場合は作成
                    var directory = Path.GetDirectoryName(localPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        LogDebug($"画像ディレクトリ作成: {directory}");
                    }

                    using var sourceStream = await result.OpenReadAsync();
                    using var localStream = File.OpenWrite(localPath);
                    await sourceStream.CopyToAsync(localStream);

                    PlayerImagePath = localPath;
                    LogDebug($"画像保存完了: {localPath}");
                }
                else
                {
                    LogDebug("画像選択をキャンセル");
                }
            }
            catch (Exception ex)
            {
                LogError("画像選択処理に失敗", ex);
                await ShowErrorAlert("画像選択エラー", $"画像の選択に失敗しました。\n{ex.Message}");
            }
        }

        [RelayCommand]
        void RemoveImage()
        {
            try
            {
                LogDebug("画像削除");
                PlayerImagePath = string.Empty;
            }
            catch (Exception ex)
            {
                LogError("画像削除処理に失敗", ex);
            }
        }

        [RelayCommand]
        async Task CancelEdit()
        {
            try
            {
                LogDebug("編集キャンセル");
                ShowEditor = false;
                ClearForm();
            }
            catch (Exception ex)
            {
                LogError("編集キャンセル処理に失敗", ex);
                await ShowErrorAlert("エラー", ex.Message);
            }
        }

        private bool ValidatePlayerData()
        {
            try
            {
                ClearValidationErrors();

                // 選手名チェック
                if (string.IsNullOrWhiteSpace(PlayerName))
                {
                    SetValidationError("選手名を入力してください");
                    return false;
                }

                if (PlayerName.Trim().Length > 50)
                {
                    SetValidationError("選手名は50文字以内で入力してください");
                    return false;
                }

                // 背番号チェック
                if (PlayerNumber < 1 || PlayerNumber > 99)
                {
                    SetValidationError("背番号は1-99の範囲で入力してください");
                    return false;
                }

                // 背番号の重複チェック
                var existingPlayer = Players.FirstOrDefault(p =>
                    p.Number == PlayerNumber &&
                    p.Id != (SelectedPlayer?.Id ?? 0));

                if (existingPlayer != null)
                {
                    SetValidationError($"背番号{PlayerNumber}は「{existingPlayer.Name}」が既に使用しています");
                    return false;
                }

                LogDebug("バリデーション完了: OK");
                return true;
            }
            catch (Exception ex)
            {
                LogError("バリデーション処理でエラー", ex);
                SetValidationError("入力内容の検証中にエラーが発生しました");
                return false;
            }
        }

        private async Task UpdatePlayersCollection(Player player)
        {
            try
            {
                if (IsEditMode)
                {
                    // 既存の選手情報を更新
                    var index = Players.ToList().FindIndex(p => p.Id == player.Id);
                    if (index >= 0)
                    {
                        Players[index] = player;
                        LogDebug($"Players collection更新: index={index}");
                    }
                }
                else
                {
                    // 新規選手を追加
                    Players.Add(player);
                    LogDebug("Players collectionに新規選手追加");
                }

                // Players を番号順で再ソート
                var sortedPlayers = Players.OrderBy(p => p.Number).ToList();
                Players.Clear();
                foreach (var p in sortedPlayers)
                {
                    Players.Add(p);
                }
                LogDebug("Players collectionソート完了");
            }
            catch (Exception ex)
            {
                LogError("Players collection更新に失敗", ex);
                // UI更新に失敗した場合は、データを再読み込み
                await LoadPlayersAsync();
            }
        }

        private void ClearForm()
        {
            try
            {
                PlayerName = string.Empty;
                PlayerPosition = "FW";
                PlayerNumber = GetNextAvailableNumber();
                PreferredFoot = "右足";
                PlayerImagePath = string.Empty;
                IsEditMode = false;
                SelectedPlayer = null;
                ClearValidationErrors();
                LogDebug("フォームクリア完了");
            }
            catch (Exception ex)
            {
                LogError("フォームクリア処理でエラー", ex);
            }
        }

        private int GetNextAvailableNumber()
        {
            try
            {
                for (int i = 1; i <= 99; i++)
                {
                    if (!Players.Any(p => p.Number == i))
                    {
                        LogDebug($"次の利用可能背番号: {i}");
                        return i;
                    }
                }
                LogDebug("利用可能背番号なし、デフォルト1を返す");
                return 1;
            }
            catch (Exception ex)
            {
                LogError("次の背番号取得でエラー", ex);
                return 1;
            }
        }

        private void SetValidationError(string message)
        {
            ValidationErrorMessage = message;
            HasValidationError = true;
            LogDebug($"バリデーションエラー: {message}");
        }

        private void ClearValidationErrors()
        {
            ValidationErrorMessage = string.Empty;
            HasValidationError = false;
        }

        private async Task ShowErrorAlert(string title, string message)
        {
            try
            {
                await Shell.Current.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                LogError($"エラーダイアログ表示に失敗 - {title}: {message}", ex);
            }
        }

        private async Task ShowSuccessAlert(string title, string message)
        {
            try
            {
                await Shell.Current.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                LogError($"成功ダイアログ表示に失敗 - {title}: {message}", ex);
            }
        }

        private void LogDebug(string message)
        {
#if DEBUG
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";

            Debug.WriteLine($"[PlayerManagement] {logMessage}");

            // UI表示用のデバッグメッセージを更新（最新10件のみ保持）
            var debugMessages = string.IsNullOrEmpty(DebugMessage) ?
                new List<string>() : DebugMessage.Split('\n').ToList();

            debugMessages.Add(logMessage);

            if (debugMessages.Count > 10)
            {
                debugMessages = debugMessages.Skip(debugMessages.Count - 10).ToList();
            }

            DebugMessage = string.Join("\n", debugMessages);

            // Players.Countも表示
            var countInfo = $"Players.Count = {Players.Count}";
            if (!logMessage.Contains("Players.Count"))
            {
                debugMessages.Add($"[{timestamp}] {countInfo}");
                if (debugMessages.Count > 10)
                {
                    debugMessages = debugMessages.Skip(debugMessages.Count - 10).ToList();
                }
                DebugMessage = string.Join("\n", debugMessages);
            }
#endif
        }

        private void LogError(string message, Exception ex = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var errorMessage = $"[{timestamp}] ERROR: {message}";

            if (ex != null)
            {
                errorMessage += $"\n例外: {ex.GetType().Name}";
                errorMessage += $"\nメッセージ: {ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部例外: {ex.InnerException.Message}";
                }

                errorMessage += $"\nスタックトレース: {ex.StackTrace}";
            }

            Debug.WriteLine($"[PlayerManagement] {errorMessage}");

#if DEBUG
            // UI表示用のエラー詳細を更新
            LastErrorDetails = errorMessage;

            // デバッグメッセージも更新
            var debugMessages = DebugMessage.Split('\n').ToList();
            debugMessages.Add($"[{timestamp}] ❌ {message}");

            if (debugMessages.Count > 5)
            {
                debugMessages = debugMessages.Skip(debugMessages.Count - 5).ToList();
            }

            DebugMessage = string.Join("\n", debugMessages);
#endif
        }

        public async Task OnAppearing()
        {
            try
            {
                LogDebug("画面表示開始");
                LogDebug($"初期状態 - Players.Count: {Players.Count}");
                LogDebug($"初期状態 - ShowEditor: {ShowEditor}");
                LogDebug($"初期状態 - IsBusy: {IsBusy}");

                await LoadPlayersAsync();

                LogDebug($"OnAppearing完了 - Players.Count: {Players.Count}");
                LogDebug("画面表示完了");
            }
            catch (Exception ex)
            {
                LogError("画面表示処理でエラー", ex);
                await ShowErrorAlert("初期化エラー", $"画面の初期化に失敗しました。\n{ex.Message}");
            }
        }

        // デバッグ用の強制リフレッシュコマンド
        [RelayCommand]
        async Task ForceRefresh()
        {
            try
            {
                LogDebug("強制リフレッシュ開始");
                await LoadPlayersAsync();
                LogDebug("強制リフレッシュ完了");
            }
            catch (Exception ex)
            {
                LogError("強制リフレッシュでエラー", ex);
            }
        }

        // エディター表示状態変更時の処理
        partial void OnShowEditorChanged(bool value)
        {
            try
            {
                LogDebug($"エディター表示状態変更: {value}");

                if (!value)
                {
                    // エディターが閉じられる時の処理
                    ClearValidationErrors();
                }
            }
            catch (Exception ex)
            {
                LogError("エディター表示状態変更でエラー", ex);
            }
        }
    }
}