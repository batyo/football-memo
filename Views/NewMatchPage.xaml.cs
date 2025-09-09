using MatchMemoApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Specialized;

namespace MatchMemoApp.Views;

public partial class NewMatchPage : ContentPage
{
    private readonly NewMatchViewModel _viewModel;
    private Grid? homeArea;
    private Grid? awayArea;

    public NewMatchPage(NewMatchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // フィールドエリアの参照を取得
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        homeArea = this.FindByName<Grid>("HomeArea");
        awayArea = this.FindByName<Grid>("AwayArea");

        // コレクション変更の監視を設定
        SetupCollectionWatchers();

        // 初期の選手配置を表示
        RefreshPlayerDisplay();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearing();
    }

    private void SetupCollectionWatchers()
    {
        // ホームチーム選手コレクションの変更を監視
        if (_viewModel.HomeTeamPlayers is INotifyCollectionChanged homeCollection)
            homeCollection.CollectionChanged += OnPlayersChanged;

        // アウェイチーム選手コレクションの変更を監視
        if (_viewModel.AwayTeamPlayers is INotifyCollectionChanged awayCollection)
            awayCollection.CollectionChanged += OnPlayersChanged;
    }

    private void OnPlayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPlayerDisplay();
    }

    /// <summary>
    /// フィールド上の選手表示を更新
    /// </summary>
    private void RefreshPlayerDisplay()
    {
        if (homeArea == null || awayArea == null) return;

        // 既存の選手アイコンをクリア
        ClearPlayerIcons(homeArea);
        ClearPlayerIcons(awayArea);

        // ホームチーム選手を表示
        foreach (var player in _viewModel.HomeTeamPlayers)
        {
            var playerView = CreatePlayerView(player);
            AddPlayerToArea(homeArea, playerView, player.X, player.Y);
        }

        // アウェイチーム選手を表示
        foreach (var player in _viewModel.AwayTeamPlayers)
        {
            var playerView = CreatePlayerView(player);
            AddPlayerToArea(awayArea, playerView, player.X, player.Y);
        }
    }

    /// <summary>
    /// エリア内の選手アイコンをクリア
    /// </summary>
    private void ClearPlayerIcons(Grid area)
    {
        var playersToRemove = area.Children
            .OfType<View>()
            .Where(v => v.StyleId == "PlayerIcon")
            .ToList();

        foreach (var player in playersToRemove)
        {
            area.Children.Remove(player);
        }
    }

    /// <summary>
    /// 選手ビューを作成
    /// </summary>
    private View CreatePlayerView(FormationPlayer player)
    {
        var mainContainer = new StackLayout
        {
            StyleId = "PlayerIcon",
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // 選手アイコン（円形）
        var playerIcon = new Border
        {
            BackgroundColor = player.TeamColor,
            WidthRequest = 35,
            HeightRequest = 35,
            StrokeThickness = 2,
            Stroke = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 17.5 },
            HorizontalOptions = LayoutOptions.Center
        };

        // アイコン内容
        var iconContent = new StackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 0
        };

        // 選手画像（低優先度機能のため後で実装）
        if (!string.IsNullOrEmpty(player.ImagePath))
        {
            // TODO: 画像表示機能は低優先度で後から実装
        }

        // 背番号表示
        var numberLabel = new Label
        {
            Text = player.DisplayNumber,
            TextColor = Colors.White,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        iconContent.Children.Add(numberLabel);
        playerIcon.Content = iconContent;
        mainContainer.Children.Add(playerIcon);

        // 選手名表示（設定済みの場合のみ）
        if (player.IsConfigured && !string.IsNullOrEmpty(player.DisplayName))
        {
            var nameLabel = new Label
            {
                Text = player.DisplayName,
                TextColor = Colors.White,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = Color.FromArgb("#80000000"), // 半透明背景
                Padding = new Thickness(4, 1)
            };

            mainContainer.Children.Add(nameLabel);
        }

        // タップイベント（選手情報編集）
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (sender, e) =>
        {
            _viewModel.EditPlayerCommand.Execute(player);
        };
        mainContainer.GestureRecognizers.Add(tapGesture);

        return mainContainer;
    }

    /// <summary>
    /// 選手をフィールドエリアに配置
    /// </summary>
    private void AddPlayerToArea(Grid area, View playerView, double xPercent, double yPercent)
    {
        area.Children.Add(playerView);

        // パーセンテージを比例値に変換して配置
        // xPercent, yPercentは0-100の範囲
        var proportionalX = xPercent / 100.0;
        var proportionalY = yPercent / 100.0;

        // Gridの比例配置を使用
        playerView.HorizontalOptions = LayoutOptions.Start;
        playerView.VerticalOptions = LayoutOptions.Start;
        playerView.Margin = new Thickness(
            proportionalX * (area.Width > 0 ? area.Width - 35 : 365), // アイコンサイズを考慮
            proportionalY * (area.Height > 0 ? area.Height - 35 : 265),
            0, 0);
    }

    /// <summary>
    /// フィールドサイズ変更時の再配置
    /// </summary>
    private void OnFieldSizeChanged(object? sender, EventArgs e)
    {
        // フィールドサイズが変わった時に選手位置を再計算
        RefreshPlayerDisplay();
    }

    /// <summary>
    /// リソースの解放
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // イベント監視の解除
        if (_viewModel.HomeTeamPlayers is INotifyCollectionChanged homeCollection)
            homeCollection.CollectionChanged -= OnPlayersChanged;

        if (_viewModel.AwayTeamPlayers is INotifyCollectionChanged awayCollection)
            awayCollection.CollectionChanged -= OnPlayersChanged;
    }
}