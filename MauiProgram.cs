using MatchMemoApp.Data;
using MatchMemoApp.ViewModels;
using MatchMemoApp.Views;
using MatchMemoApp.Services;
using Microsoft.Extensions.Logging;

namespace MatchMemoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if DEBUG
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });
#endif

        // サービス登録
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<ISpeechService, SpeechService>();

        // ViewModel登録
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<NewMatchViewModel>();
        builder.Services.AddTransient<MatchDetailViewModel>();
        builder.Services.AddTransient<PlayerManagementViewModel>();

        // View登録
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<NewMatchPage>();
        builder.Services.AddTransient<MatchDetailPage>();
        builder.Services.AddTransient<NewMatchViewModel>();
        builder.Services.AddTransient<PlayerManagementPage>();

        return builder.Build();
    }
}