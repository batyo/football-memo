using MatchMemoApp.ViewModels;
using System.Diagnostics;

namespace MatchMemoApp.Views;

public partial class PlayerManagementPage : ContentPage
{
    private readonly PlayerManagementViewModel _viewModel;

    public PlayerManagementPage(PlayerManagementViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            Debug.WriteLine("[PlayerManagementPage] ページ初期化完了");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PlayerManagementPage] 初期化エラー: {ex.Message}");
            Debug.WriteLine($"[PlayerManagementPage] スタックトレース: {ex.StackTrace}");
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        try
        {
            Debug.WriteLine("[PlayerManagementPage] OnAppearing開始");
            base.OnAppearing();

            if (_viewModel != null)
            {
                // CollectionViewの描画問題対応のため、少し遅延してからデータ読み込み
                await Task.Delay(200);
                await _viewModel.OnAppearing();
                Debug.WriteLine("[PlayerManagementPage] ViewModel.OnAppearing完了");

                // さらに遅延してからUI強制更新
                await Task.Delay(300);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        // BindingContextを一度リセットして再設定（CollectionView問題の回避）
                        var currentViewModel = BindingContext;
                        BindingContext = null;
                        BindingContext = currentViewModel;
                        Debug.WriteLine("[PlayerManagementPage] BindingContext再設定完了");
                    }
                    catch (Exception bindEx)
                    {
                        Debug.WriteLine($"[PlayerManagementPage] BindingContext再設定エラー: {bindEx.Message}");
                    }
                });
            }
            else
            {
                Debug.WriteLine("[PlayerManagementPage] 警告: ViewModelがnull");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PlayerManagementPage] OnAppearing エラー: {ex.Message}");
            Debug.WriteLine($"[PlayerManagementPage] スタックトレース: {ex.StackTrace}");

            // ユーザーにエラーを通知
            await DisplayAlert(
                "初期化エラー",
                $"画面の初期化中にエラーが発生しました。\n\n詳細: {ex.Message}",
                "OK");
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            Debug.WriteLine("[PlayerManagementPage] OnDisappearing開始");

            // エディターが開いている場合は閉じる
            if (_viewModel?.ShowEditor == true)
            {
                _viewModel.ShowEditor = false;
                Debug.WriteLine("[PlayerManagementPage] エディターを強制的に閉じました");
            }

            base.OnDisappearing();
            Debug.WriteLine("[PlayerManagementPage] OnDisappearing完了");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PlayerManagementPage] OnDisappearing エラー: {ex.Message}");
        }
    }

    // Android バックボタン対応
    protected override bool OnBackButtonPressed()
    {
        try
        {
            Debug.WriteLine("[PlayerManagementPage] バックボタン押下");

            // エディターが開いている場合はエディターを閉じる
            if (_viewModel?.ShowEditor == true)
            {
                Debug.WriteLine("[PlayerManagementPage] エディターを閉じてバックボタンをキャンセル");
                _viewModel.ShowEditor = false;
                return true; // バックナビゲーションをキャンセル
            }

            Debug.WriteLine("[PlayerManagementPage] 通常のバックナビゲーションを実行");
            return base.OnBackButtonPressed();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PlayerManagementPage] バックボタン処理エラー: {ex.Message}");
            return base.OnBackButtonPressed();
        }
    }
}