#if IOS
using AVFoundation;
using Foundation;
using Speech;

namespace MatchMemoApp.Platforms.iOS
{
    public class SpeechRecognitionService
    {
        private SFSpeechRecognizer _speechRecognizer;
        private SFSpeechAudioBufferRecognitionRequest _recognitionRequest;
        private SFSpeechRecognitionTask _recognitionTask;
        private AVAudioEngine _audioEngine;

        public SpeechRecognitionService()
        {
            _speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier("ja-JP"));
            _audioEngine = new AVAudioEngine();
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            // Speech Recognition permission (コールバック→Task化)
            var speechTcs = new TaskCompletionSource<SFSpeechRecognizerAuthorizationStatus>();
            SFSpeechRecognizer.RequestAuthorization(status => speechTcs.SetResult(status));
            var speechStatus = await speechTcs.Task;
            if (speechStatus != SFSpeechRecognizerAuthorizationStatus.Authorized)
                return false;

            // Microphone permission (コールバック→Task化)
            var micTcs = new TaskCompletionSource<bool>();
            AVAudioSession.SharedInstance().RequestRecordPermission(granted => micTcs.SetResult(granted));
            var micStatus = await micTcs.Task;
            return micStatus;
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            var hasPermission = await RequestPermissionsAsync();
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("音声認識の許可が必要です");
            }

            if (_speechRecognizer == null || !_speechRecognizer.Available)
            {
                throw new NotSupportedException("音声認識が利用できません");
            }

            var tcs = new TaskCompletionSource<string>();
            string lastTranscription = string.Empty; // ★ 追加

            try
            {
                // Cancel any ongoing recognition
                if (_recognitionTask != null)
                {
                    _recognitionTask.Cancel();
                    _recognitionTask = null;
                }

                var audioSession = AVAudioSession.SharedInstance();

                // SetCategory (同期)
                audioSession.SetCategory(AVAudioSessionCategory.Record, (AVAudioSessionCategoryOptions)0, out var setCategoryError);
                if (setCategoryError != null)
                {
                    throw new Exception($"AudioSession set category error: {setCategoryError.LocalizedDescription}");
                }

                // SetActive (同期)
                audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out var setActiveError);
                if (setActiveError != null)
                {
                    throw new Exception($"AudioSession set active error: {setActiveError.LocalizedDescription}");
                }

                _recognitionRequest = new SFSpeechAudioBufferRecognitionRequest();
                _recognitionRequest.ShouldReportPartialResults = true;

                var inputNode = _audioEngine.InputNode;
                var recordingFormat = inputNode.GetBusOutputFormat(0);

                inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) =>
                {
                    _recognitionRequest.Append(buffer);
                });

                _audioEngine.Prepare();
                _audioEngine.StartAndReturnError(out var error);

                if (error != null)
                {
                    throw new Exception($"AudioEngine start error: {error.LocalizedDescription}");
                }

                _recognitionTask = _speechRecognizer.GetRecognitionTask(_recognitionRequest, (result, recognitionError) =>
                {
                    if (result != null)
                    {
                        if (result.Final)
                        {
                            tcs.TrySetResult(result.BestTranscription.FormattedString);
                            StopRecording();
                        }
                    }

                    if (recognitionError != null)
                    {
                        tcs.TrySetException(new Exception(recognitionError.LocalizedDescription));
                        StopRecording();
                    }
                });

                // 5秒後に自動停止
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        StopRecording();
                        //tcs.TrySetResult(_recognitionTask?.Result?.BestTranscription?.FormattedString ?? string.Empty);
                        tcs.TrySetResult(lastTranscription); // ★ ここで返す
                    }
                });

                return await tcs.Task;
            }
            catch (Exception)
            {
                StopRecording();
                throw;
            }
        }

        private void StopRecording()
        {
            _audioEngine.Stop();
            _audioEngine.InputNode.RemoveTapOnBus(0);
            _recognitionRequest?.EndAudio();
            _recognitionTask?.Cancel();
        }
    }
}
#endif