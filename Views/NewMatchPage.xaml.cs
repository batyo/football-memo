using MatchMemoApp.ViewModels;
using Microsoft.Maui.Layouts;
using System.Collections.Specialized;

namespace MatchMemoApp.Views;

public partial class NewMatchPage : ContentPage
{
    private readonly NewMatchViewModel _viewModel;
    private AbsoluteLayout fieldLayout;

    public NewMatchPage(NewMatchViewModel viewModel)
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

    private void SetupCollectionWatchers()
    {
        // ホームチーム選手コレクションの変更を監視
        if (_viewModel.HomeTeamPlayers is INotifyCollectionChanged homeCollection)
            homeCollection.CollectionChanged += OnHomePlayersChanged;

        // アウェイチーム選手コレクションの変更を監視
        if (_viewModel.AwayTeamPlayers is INotifyCollectionChanged awayCollection)
            awayCollection.CollectionChanged += OnAwayPlayersChanged;
    }

    private void OnHomePlayersChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPlayerDisplay();
    }

    private void OnAwayPlayersChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPlayerDisplay();
    }

    private void RefreshPlayerDisplay()
    {
        if (fieldLayout == null || _viewModel == null) return;

        // 既存の選手要素をクリア（StyleIdを持つView要素のみを対象とする）
        var playersToRemove = fieldLayout.Children
            .OfType<View>()
            .Where(c => c.StyleId == "Player")
            .ToList();

        foreach (var player in playersToRemove)
        {
            fieldLayout.Children.Remove(player);
        }

        // ホームチーム選手を追加
        foreach (var fieldPlayer in _viewModel.HomeTeamPlayers)
        {
            var playerFrame = CreatePlayerFrame(fieldPlayer, Color.FromArgb("#2196F3"));
            fieldLayout.Children.Add(playerFrame);

            // 位置を設定（パーセンテージを比例値に変換）
            var bounds = new Rect(fieldPlayer.X / 100.0, fieldPlayer.Y / 100.0, -1, -1);
            AbsoluteLayout.SetLayoutBounds(playerFrame, bounds);
            AbsoluteLayout.SetLayoutFlags(playerFrame, AbsoluteLayoutFlags.PositionProportional);
        }

        // アウェイチーム選手を追加
        foreach (var fieldPlayer in _viewModel.AwayTeamPlayers)
        {
            var playerFrame = CreatePlayerFrame(fieldPlayer, Color.FromArgb("#F44336"));
            fieldLayout.Children.Add(playerFrame);

            // 位置を設定（パーセンテージを比例値に変換）
            var bounds = new Rect(fieldPlayer.X / 100.0, fieldPlayer.Y / 100.0, -1, -1);
            AbsoluteLayout.SetLayoutBounds(playerFrame, bounds);
            AbsoluteLayout.SetLayoutFlags(playerFrame, AbsoluteLayoutFlags.PositionProportional);
        }
    }

    private Frame CreatePlayerFrame(dynamic fieldPlayer, Color backgroundColor)
    {
        var frame = new Frame
        {
            BackgroundColor = backgroundColor,
            WidthRequest = 35,
            HeightRequest = 35,
            CornerRadius = 17.5f,
            HasShadow = true,
            Padding = 0,
            StyleId = "Player" // 識別用
        };

        var label = new Label
        {
            Text = fieldPlayer.Player.Number.ToString(),
            TextColor = Colors.White,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        frame.Content = label;

        // タップイベント（選手削除用）
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (sender, e) =>
        {
            _viewModel?.RemovePlayerFromFieldCommand?.Execute(fieldPlayer);
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    private void OnHomeFormationChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedItem is string formation)
        {
            _viewModel.SetHomeTeamFormationCommand.Execute(formation);
            // フォーメーション変更後に選手配置を更新
            RefreshPlayerDisplay();
        }
    }

    private void OnAwayFormationChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedItem is string formation)
        {
            _viewModel.SetAwayTeamFormationCommand.Execute(formation);
            // フォーメーション変更後に選手配置を更新
            RefreshPlayerDisplay();
        }
    }

    // リソースの解放
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // コレクション変更監視の解除
        if (_viewModel.HomeTeamPlayers is INotifyCollectionChanged homeCollection)
            homeCollection.CollectionChanged -= OnHomePlayersChanged;

        if (_viewModel.AwayTeamPlayers is INotifyCollectionChanged awayCollection)
            awayCollection.CollectionChanged -= OnAwayPlayersChanged;
    }
}