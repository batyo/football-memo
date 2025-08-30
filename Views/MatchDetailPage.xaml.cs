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

        // ホームチーム選手を配置（下半分、上向き）
        foreach (var fieldPlayer in _viewModel.HomeTeamPlayersOnField)
        {
            CreateAndAddPlayerViews(fieldPlayer, Color.FromArgb("#2196F3"), true);
        }

        // アウェイチーム選手を配置（上半分、下向き）
        foreach (var fieldPlayer in _viewModel.AwayTeamPlayersOnField)
        {
            CreateAndAddPlayerViews(fieldPlayer, Color.FromArgb("#F44336"), false);
        }
    }

    private void CreateAndAddPlayerViews(FieldPlayerView fieldPlayer, Color teamColor, bool isHomeTeam)
    {
        // フォーメーション配置を考慮した位置調整
        double adjustedX, adjustedY;
        CalculateFormationPosition(fieldPlayer, isHomeTeam, out adjustedX, out adjustedY);

        // 選手ボタンを作成
        var playerButton = CreatePlayerButton(fieldPlayer, teamColor);
        fieldLayout.Children.Add(playerButton);

        // ボタンの位置を設定（パーセンテージを比例値に変換）
        var buttonBounds = new Rect(adjustedX / 100.0, adjustedY / 100.0, -1, -1);
        AbsoluteLayout.SetLayoutBounds(playerButton, buttonBounds);
        AbsoluteLayout.SetLayoutFlags(playerButton, AbsoluteLayoutFlags.PositionProportional);

        // 選手名・背番号ラベルを作成
        var playerInfoLabel = CreatePlayerInfoLabel(fieldPlayer);
        fieldLayout.Children.Add(playerInfoLabel);

        // 情報ラベルの位置を設定（ボタンの下に配置）
        var infoBounds = new Rect(adjustedX / 100.0, (adjustedY + 8) / 100.0, -1, -1);
        AbsoluteLayout.SetLayoutBounds(playerInfoLabel, infoBounds);
        AbsoluteLayout.SetLayoutFlags(playerInfoLabel, AbsoluteLayoutFlags.PositionProportional);
    }

    private void CalculateFormationPosition(FieldPlayerView fieldPlayer, bool isHomeTeam, out double adjustedX, out double adjustedY)
    {
        // 元の位置（0-100%）
        double originalX = fieldPlayer.X;
        double originalY = fieldPlayer.Y;

        // 横方向は最大限に広げる（マージンを考慮）
        double minX = 10.0; // 左マージン
        double maxX = 90.0; // 右マージン
        adjustedX = minX + (originalX / 100.0 * (maxX - minX));

        if (isHomeTeam)
        {
            // ホームチーム: 下半分に配置（50%-95%）
            // GK -> 最も下（95%付近）
            // FW -> ハーフライン付近（55%付近）
            double homeMinY = 55.0; // ハーフラインより少し下
            double homeMaxY = 95.0; // 最下端近く

            // 元の位置に基づいて縦方向を調整
            // originalY が 0% (GK) なら homeMaxY に、100% (FW) なら homeMinY に配置
            adjustedY = homeMaxY - (originalY / 100.0 * (homeMaxY - homeMinY));
        }
        else
        {
            // アウェイチーム: 上半分に配置（5%-45%）
            // FW -> ハーフライン付近（45%付近）  
            // GK -> 最も上（5%付近）
            double awayMinY = 5.0;  // 最上端近く
            double awayMaxY = 45.0; // ハーフラインより少し上

            // 元の位置に基づいて縦方向を調整（上下反転）
            // originalY が 100% (FW) なら awayMaxY に、0% (GK) なら awayMinY に配置
            adjustedY = awayMinY + (originalY / 100.0 * (awayMaxY - awayMinY));
        }
    }

    private Button CreatePlayerButton(FieldPlayerView fieldPlayer, Color teamColor)
    {
        var playerButton = new Button
        {
            StyleId = "PlayerIcon",
            BackgroundColor = teamColor,
            TextColor = Colors.White,
            Text = fieldPlayer.Player.Number.ToString(),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = 0,
            Margin = 0,
            BorderColor = Colors.White,
            BorderWidth = 2
        };

        // コマンドを直接バインド
        playerButton.Command = _viewModel.SelectPlayerFromFieldCommand;
        playerButton.CommandParameter = fieldPlayer.Player;

        return playerButton;
    }

    private Label CreatePlayerInfoLabel(FieldPlayerView fieldPlayer)
    {
        var infoLabel = new Label
        {
            Text = $"{fieldPlayer.Player.Name}\n#{fieldPlayer.Player.Number}",
            TextColor = Colors.White,
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            StyleId = "PlayerName"
        };

        return infoLabel;
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