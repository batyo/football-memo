using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MatchMemoApp.Data;
using MatchMemoApp.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;

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
        string playerName = string.Empty;

        [ObservableProperty]
        string playerPosition = "FW";

        [ObservableProperty]
        int playerNumber = 1;

        [ObservableProperty]
        string preferredFoot = "右足";

        [ObservableProperty]
        string playerImagePath = string.Empty;

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
        }

        [RelayCommand]
        async Task LoadPlayersAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var players = await _databaseService.GetPlayersAsync();

                Players.Clear();
                foreach (var player in players.OrderBy(p => p.Number))
                {
                    Players.Add(player);
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
        void NewPlayer()
        {
            ClearForm();
            IsEditMode = false;
            SelectedPlayer = null;
        }

        [RelayCommand]
        void EditPlayer(Player player)
        {
            if (player == null) return;

            SelectedPlayer = player;
            PlayerName = player.Name;
            PlayerPosition = player.Position;
            PlayerNumber = player.Number;
            PreferredFoot = player.PreferredFoot;
            PlayerImagePath = player.ImagePath ?? string.Empty;
            IsEditMode = true;
        }

        [RelayCommand]
        async Task SavePlayerAsync()
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                await Shell.Current.DisplayAlert("エラー", "選手名を入力してください", "OK");
                return;
            }

            if (PlayerNumber < 1 || PlayerNumber > 99)
            {
                await Shell.Current.DisplayAlert("エラー", "背番号は1-99の範囲で入力してください", "OK");
                return;
            }

            // 背番号の重複チェック
            var existingPlayer = Players.FirstOrDefault(p => p.Number == PlayerNumber && p.Id != (SelectedPlayer?.Id ?? 0));
            if (existingPlayer != null)
            {
                await Shell.Current.DisplayAlert("エラー", $"背番号{PlayerNumber}は既に使用されています", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                Player player;
                if (IsEditMode && SelectedPlayer != null)
                {
                    // 編集モード
                    player = SelectedPlayer;
                    player.Name = PlayerName;
                    player.Position = PlayerPosition;
                    player.Number = PlayerNumber;
                    player.PreferredFoot = PreferredFoot;
                    player.ImagePath = PlayerImagePath;
                }
                else
                {
                    // 新規作成モード
                    player = new Player
                    {
                        Name = PlayerName,
                        Position = PlayerPosition,
                        Number = PlayerNumber,
                        PreferredFoot = PreferredFoot,
                        ImagePath = PlayerImagePath
                    };
                }

                await _databaseService.SavePlayerAsync(player);

                // UI更新
                if (IsEditMode)
                {
                    // 既存の選手情報を更新
                    var index = Players.IndexOf(SelectedPlayer);
                    if (index >= 0)
                    {
                        Players[index] = player;
                    }
                }
                else
                {
                    // 新規選手を追加
                    Players.Add(player);
                }

                // Players を番号順で再ソート
                var sortedPlayers = Players.OrderBy(p => p.Number).ToList();
                Players.Clear();
                foreach (var p in sortedPlayers)
                {
                    Players.Add(p);
                }

                ClearForm();
                await Shell.Current.DisplayAlert("成功", $"選手情報を{(IsEditMode ? "更新" : "保存")}しました", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"選手情報の保存に失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task DeletePlayerAsync(Player player)
        {
            if (player == null) return;

            bool result = await Shell.Current.DisplayAlert(
                "確認",
                $"{player.Name}を削除しますか？\n※この操作は元に戻せません",
                "削除",
                "キャンセル");

            if (!result) return;

            try
            {
                IsBusy = true;

                // データベースから削除
                await _databaseService.DeletePlayerAsync(player.Id);

                // UI更新
                Players.Remove(player);

                // 編集中だった場合はフォームをクリア
                if (SelectedPlayer?.Id == player.Id)
                {
                    ClearForm();
                }

                await Shell.Current.DisplayAlert("成功", "選手を削除しました", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"選手の削除に失敗しました: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task SelectImageAsync()
        {
            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync();
                if (result != null)
                {
                    // アプリのローカルフォルダにファイルをコピー
                    var fileName = $"player_{DateTime.Now.Ticks}.jpg";
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, "PlayerImages", fileName);

                    // ディレクトリが存在しない場合は作成
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                    using var sourceStream = await result.OpenReadAsync();
                    using var localStream = File.OpenWrite(localPath);
                    await sourceStream.CopyToAsync(localStream);

                    PlayerImagePath = localPath;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("エラー", $"画像の選択に失敗しました: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        void RemoveImage()
        {
            PlayerImagePath = string.Empty;
        }

        [RelayCommand]
        void CancelEdit()
        {
            ClearForm();
        }

        private void ClearForm()
        {
            PlayerName = string.Empty;
            PlayerPosition = "FW";
            PlayerNumber = GetNextAvailableNumber();
            PreferredFoot = "右足";
            PlayerImagePath = string.Empty;
            IsEditMode = false;
            SelectedPlayer = null;
        }

        private int GetNextAvailableNumber()
        {
            for (int i = 1; i <= 99; i++)
            {
                if (!Players.Any(p => p.Number == i))
                {
                    return i;
                }
            }
            return 1;
        }

        public async Task OnAppearing()
        {
            await LoadPlayersAsync();
        }
    }
}