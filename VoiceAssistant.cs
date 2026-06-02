using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vosk;

namespace Jarvis
{
    public class VoiceAssistant
    {
        private readonly List<MediaPlayer> _activePlayers = new List<MediaPlayer>();
        private bool _isListening = false;
        private VoskRecognizer? _voskRecognizer;
        private WaveInEvent? _waveIn;
        public bool IsListening => _isListening;

        private readonly string _apiKey = GetConfig("ElevenLabsApiKey");
        private readonly string _voiceId = GetConfig("ElevenLabsVoiceId");

        private static string GetConfig(string key)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return "";
            var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return json.RootElement.GetProperty(key).GetString() ?? "";
        }

        public VoiceAssistant()
        {
            
        }

        public async Task SpeakAsync(string text)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
                    client.DefaultRequestHeaders.Add("User-Agent", "JarvisApp/1.0");

                    string url = $"https://api.elevenlabs.io/v1/text-to-speech/{_voiceId}";

                    string jsonBody = $@"{{
                        ""text"": ""{text}"",
                        ""model_id"": ""eleven_multilingual_v2"",
                        ""voice_settings"": {{
                            ""stability"": 0.5,
                            ""similarity_boost"": 0.75
                        }}
                    }}";

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                        string tempFile = Path.Combine(Path.GetTempPath(), $"jarvis_{Guid.NewGuid()}.mp3");
                        File.WriteAllBytes(tempFile, audioBytes);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // front: додатковий плеєр з авто-видаленням temp-файлу
                            MediaPlayer player = new MediaPlayer();
                            player.MediaEnded += (s, e) =>
                            {
                                _activePlayers.Remove(player);
                                try { File.Delete(tempFile); } catch { }
                            };
                            _activePlayers.Add(player);
                            player.Open(new Uri(tempFile));
                            player.Play();
                        });
                    }
                    else
                    {
                        string errorMsg = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Помилка ElevenLabs (Код {response.StatusCode}):\n{errorMsg}", "Помилка API");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка мережі: {ex.Message}", "Помилка");
            }
        }

        public async Task GreetUserAsync()
        {
            int hour = DateTime.Now.Hour;
            string greeting;

            if (hour >= 5 && hour < 12)
                greeting = "Добрий ранок, сер. Система готова до роботи.";
            else if (hour >= 12 && hour < 18)
                greeting = "Добрий день, сер. Усі системи працюють штатно.";
            else
                greeting = "Добрий вечір, сер. Радий вас бачити.";

            await SpeakAsync(greeting);
        }

        public void InitSpeech(Action<string> onCommandRecognized)
        {
            try
            {
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model");

                if (!Directory.Exists(modelPath))
                {
                    MessageBox.Show($"Папка моделі не знайдена: {modelPath}", "Помилка");
                    return;
                }

                Vosk.Vosk.SetLogLevel(-1);
                var model = new Model(modelPath);
                _voskRecognizer = new VoskRecognizer(model, 16000.0f,
                    "[\"джарвіс\", \"джарвис\", \"блокнот\", \"калькулятор\", \"браузер\", \"час\", \"година\", \"привіт\", \"закрий\", \"[unk]\"]");

                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(16000, 1);

                _waveIn.DataAvailable += (s, e) =>
                {
                    if (_voskRecognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        var result = _voskRecognizer.Result();
                        var text = System.Text.Json.JsonDocument.Parse(result)
                                   .RootElement.GetProperty("text").GetString()?.ToLower() ?? "";

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            onCommandRecognized(text);
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації мікрофона: {ex.Message}", "Помилка");
            }
        }

        public void StartListening()
        {
            if (_waveIn != null && !_isListening)
            {
                _waveIn.StartRecording();
                _isListening = true;
            }
        }

        public void StopListening()
        {
            if (_waveIn != null && _isListening)
            {
                _waveIn.StopRecording();
                _isListening = false;
            }
        }
    }
}