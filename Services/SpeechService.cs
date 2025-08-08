using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace MatchMemoApp.Services
{
    public interface ISpeechService
    {
        Task<string> RecognizeSpeechAsync();
        Task<bool> RequestPermissionsAsync();
        bool IsSupported { get; }
        event EventHandler<string> SpeechRecognized;
        event EventHandler<string> SpeechRecognizing;
        event EventHandler SpeechStarted;
        event EventHandler SpeechEnded;
    }

    public class SpeechService : ISpeechService
    {
        private SpeechRecognizer _speechRecognizer;
        private readonly SpeechConfig _speechConfig;

        public event EventHandler<string> SpeechRecognized;
        public event EventHandler<string> SpeechRecognizing;
        public event EventHandler SpeechStarted;
        public event EventHandler SpeechEnded;

        public bool IsSupported => DeviceInfo.Platform == DevicePlatform.Android ||
                                  DeviceInfo.Platform == DevicePlatform.iOS;

        public SpeechService()
        {
            try
            {
                // Azure Speech Service の設定
                // 本番環境では設定ファイルやセキュアストレージから取得することを推奨
                var speechKey = "YOUR_SPEECH_KEY";
                var speechRegion = "japaneast";

                _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
                _speechConfig.SpeechRecognitionLanguage = "ja-JP";

                // オフライン認識を使用する場合（Androidのみ）
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // デバイス内音声認識を使用
                    InitializeDeviceSpeechRecognition();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechService initialization error: {ex.Message}");
            }
        }

        private void InitializeDeviceSpeechRecognition()
        {
            // プラットフォーム固有の音声認識を後で実装
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission request error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException("このデバイスでは音声認識がサポートされていません");
            }

            var hasPermission = await RequestPermissionsAsync();
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("マイクのアクセス許可が必要です");
            }

            try
            {
                // デバイス固有の音声認識を使用
                return await RecognizeWithDeviceAPI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition error: {ex.Message}");
                throw new Exception($"音声認識に失敗しました: {ex.Message}");
            }
        }

        private async Task<string> RecognizeWithDeviceAPI()
        {
#if ANDROID
            return await RecognizeAndroidAsync();
#elif IOS
            return await RecognizeIOSAsync();
#else
            throw new NotSupportedException("このプラットフォームはサポートされていません");
#endif
        }

#if ANDROID
        private async Task<string> RecognizeAndroidAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                var intent = new Android.Content.Intent(Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
                intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguageModel,
                               Android.Speech.RecognizerIntent.LanguageModelFreeForm);
                intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguage, "ja-JP");
                intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPrompt, "メモを音声で入力してください");

                var activity = Platform.CurrentActivity ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

                if (activity != null)
                {
                    SpeechStarted?.Invoke(this, EventArgs.Empty);

                    // ActivityResultを使用した音声認識の実装
                    // 実際の実装では ActivityResult を適切に処理する必要があります
                    activity.StartActivityForResult(intent, 1001);

                    // 簡略化のため、ここでは固定文字列を返します
                    await Task.Delay(3000); // 音声認識をシミュレート
                    SpeechEnded?.Invoke(this, EventArgs.Empty);

                    return "音声認識のテスト結果"; // 実際の実装では認識結果を返す
                }

                throw new Exception("アクティビティが見つかりません");
            }
            catch (Exception ex)
            {
                SpeechEnded?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }
#endif

#if IOS
        private async Task<string> RecognizeIOSAsync()
        {
            try
            {
                SpeechStarted?.Invoke(this, EventArgs.Empty);
                
                // iOS の Speech Framework を使用した実装
                // 実際の実装では Speech Framework を使用します
                await Task.Delay(3000); // 音声認識をシミュレート
                
                SpeechEnded?.Invoke(this, EventArgs.Empty);
                
                return "音声認識のテスト結果"; // 実際の実装では認識結果を返す
            }
            catch (Exception ex)
            {
                SpeechEnded?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }
#endif

        public void Dispose()
        {
            _speechRecognizer?.Dispose();
        }
    }
}