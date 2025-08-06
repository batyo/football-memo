using MatchMemoApp.Views;

namespace MatchMemoApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // ルート登録
        Routing.RegisterRoute(nameof(NewMatchPage), typeof(NewMatchPage));
        Routing.RegisterRoute(nameof(MatchDetailPage), typeof(MatchDetailPage));
        Routing.RegisterRoute(nameof(PlayerManagementPage), typeof(PlayerManagementPage));
    }
}