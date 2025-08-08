#if ANDROID
using Android.App;
using Android.Content;
using Android.Speech;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using Java.Util;

namespace MatchMemoApp.Platforms.Android
{
    public class SpeechRecognitionService
    {
        private readonly Context _context;
        private TaskCompletionSource<string> _tcs;

        public SpeechRecognitionService(Context context)
        {
            _context = context;
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            _tcs = new TaskCompletionSource<string>();

            try
            {
                var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                //intent.PutExtra(RecognizerIntent.ExtraLanguage, Locale.Japanese.ToString());
                intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Japanese.ToString());
                intent.PutExtra(RecognizerIntent.ExtraPrompt, "メモを音声で入力してください");
                intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

                var activity = Platform.CurrentActivity;
                if (activity is AndroidX.AppCompat.App.AppCompatActivity appCompatActivity)
                {
                    var launcher = appCompatActivity.RegisterForActivityResult(
                        new ActivityResultContracts.StartActivityForResult(),
                        new SpeechActivityResultCallback(_tcs));

                    launcher.Launch(intent);
                }
                else
                {
                    throw new Exception("適切なアクティビティが見つかりません");
                }

                return await _tcs.Task;
            }
            catch (Exception ex)
            {
                _tcs?.TrySetException(ex);
                throw;
            }
        }
    }

    public class SpeechActivityResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        private readonly TaskCompletionSource<string> _tcs;

        public SpeechActivityResultCallback(TaskCompletionSource<string> tcs)
        {
            _tcs = tcs;
        }

        public void OnActivityResult(Java.Lang.Object result)
        {
            if (result is AndroidX.Activity.Result.ActivityResult activityResult)
            {
                if (activityResult.ResultCode == (int)Result.Ok && activityResult.Data != null)
                {
                    var results = activityResult.Data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                    if (results != null && results.Count > 0)
                    {
                        _tcs.TrySetResult(results[0]);
                        return;
                    }
                }

                _tcs.TrySetResult(string.Empty);
            }
        }
    }
}
#endif