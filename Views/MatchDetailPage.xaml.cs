using MatchMemoApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using System.Collections.Specialized;

namespace MatchMemoApp.Views;

public partial class MatchDetailPage : ContentPage
{
    private readonly MatchDetailViewModel _viewModel;
    private AbsoluteLayout fieldLayout;

    public MatchDetailPage(MatchDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // フィールドレイアウトの参照を取得
        fieldLayout = this.FindByName<AbsoluteLayout>("FieldLayout");

        // コレクション変更の監視を設定
        SetupCollectionWatchers();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearing();

        // 初期の選手配置を表示
        RefreshPlayerDisplay();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private void SetupCollectionWatchers()
    {
        // ホームチーム選手コレクションの変更を監視
        if (_viewModel.HomeTeamPlayersOnField is INotifyCollectionChanged homeCollection)
            homeCollection.CollectionChanged += OnFieldPlayersChanged;

        // アウェイチーム選手コレクションの変更を監視
        if (_viewModel.AwayTeamPlayersOnField is INotifyCollectionChanged awayCollection)
            awayCollection.CollectionChanged += OnFieldPlayersChanged;
    }

    private void OnFieldPlayersChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshPlayerDisplay();
        });
    }

    private void RefreshPlayerDisplay()
    {
        if (fieldLayout == null || _viewModel == null) return;

        // 既存の選手要素をクリア（StyleIdを持つView要素のみを対象とする）
        var playersToRemove = fieldLayout.Children
            .OfType<View>()
            .Where(c => c.StyleId == "PlayerIcon" || c.StyleId == "PlayerName")
            .ToList();

        foreach (var player in playersToRemove)
        {
            fieldLayout.Children.Remove(player);
        }

        // ホームチーム選手を追加
        foreach (var fieldPlayer in _viewModel.HomeTeamPlayersOnField)
        {
            CreateAndAddPlayerViews(fieldPlayer, Color.FromArgb("#2196F3"));
        }

        // アウェイチーム選手を追加
        foreach (var fieldPlayer in _viewModel.AwayTeamPlayersOnField)
        {
            CreateAndAddPlayerViews(fieldPlayer, Color.FromArgb("#F44336"));
        }
    }

    private void CreateAndAddPlayerViews(FieldPlayerView fieldPlayer, Color teamColor)
    {
        // 選手ボタンを作成
        var playerButton = CreatePlayerButton(fieldPlayer, teamColor);
        fieldLayout.Children.Add(playerButton);

        // ボタンの位置を設定（パーセンテージを比例値に変換）
        var buttonBounds = new Rect(fieldPlayer.X / 100.0, fieldPlayer.Y / 100.0, -1, -1);
        AbsoluteLayout.SetLayoutBounds(playerButton, buttonBounds);
        AbsoluteLayout.SetLayoutFlags(playerButton, AbsoluteLayoutFlags.PositionProportional);

        // 選手名ラベルを作成
        var playerNameLabel = CreatePlayerNameLabel(fieldPlayer);
        fieldLayout.Children.Add(playerNameLabel);

        // 名前ラベルの位置を設定（ボタンの下に配置）
        var nameBounds = new Rect(fieldPlayer.X / 100.0, (fieldPlayer.Y + 12) / 100.0, -1, -1);
        AbsoluteLayout.SetLayoutBounds(playerNameLabel, nameBounds);
        AbsoluteLayout.SetLayoutFlags(playerNameLabel, AbsoluteLayoutFlags.PositionProportional);
    }

    private Button CreatePlayerButton(FieldPlayerView fieldPlayer, Color teamColor)
    {
        // Buttonを直接使用してタップイベントの問題を確実に回避
        var playerButton = new Button
        {
            StyleId = "PlayerIcon",
            BackgroundColor = teamColor,
            TextColor = Colors.White,
            Text = fieldPlayer.Player.Number.ToString(),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 50,
            HeightRequest = 50,
            CornerRadius = (int)25f,
            Padding = 0,
            Margin = 0,
            BorderColor = Colors.White,
            BorderWidth = 2
        };

        // 画像がある場合は背景に設定
        if (!string.IsNullOrEmpty(fieldPlayer.Player.ImagePath) && File.Exists(fieldPlayer.Player.ImagePath))
        {
            // TODO: 画像対応は後で実装
            playerButton.Text = fieldPlayer.Player.Number.ToString();
        }

        // コマンドを直接バインド
        playerButton.Command = _viewModel.SelectPlayerFromFieldCommand;
        playerButton.CommandParameter = fieldPlayer.Player;

        return playerButton;
    }

    private Label CreatePlayerNameLabel(FieldPlayerView fieldPlayer)
    {
        var nameLabel = new Label
        {
            Text = fieldPlayer.Player.Name,
            TextColor = Colors.White,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#CC000000"), // 半透明背景
            Padding = new Thickness(6, 2),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StyleId = "PlayerName"
        };

        return nameLabel;
    }

    // Editor のフォーカスイベントハンドラ
    private void OnMemoEditorFocused(object sender, FocusEventArgs e)
    {
        _viewModel.OnMemoEditorFocused();
    }

    private void OnMemoEditorUnfocused(object sender, FocusEventArgs e)
    {
        _viewModel.OnMemoEditorUnfocused();
    }

    // リソースの解放
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
        {
            // コレクション変更監視の解除
            if (_viewModel?.HomeTeamPlayersOnField is INotifyCollectionChanged homeCollection)
                homeCollection.CollectionChanged -= OnFieldPlayersChanged;

            if (_viewModel?.AwayTeamPlayersOnField is INotifyCollectionChanged awayCollection)
                awayCollection.CollectionChanged -= OnFieldPlayersChanged;
        }
    }
}