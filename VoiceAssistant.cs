using Microsoft.VisualBasic;
using NAudio.Wave;
using System;
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
        private readonly MediaPlayer _player;
        private bool _isListening = false;
        private VoskRecognizer? _voskRecognizer;
        private WaveInEvent? _waveIn;
        public bool IsListening => _isListening;

        private readonly string _apiKey = "sk_65a234da2e4e40e4f3cfe0804be4d26bbbbd7d94030c7652";
        private readonly string _voiceId = "ErXwobaYiN019PkySvjV";

        public VoiceAssistant()
        {
            _player = new MediaPlayer();
            _player.Volume = 1.0;

            _player.MediaFailed += (s, e) =>
            {
                MessageBox.Show($"Плеєр не зміг відтворити файл!\nПричина: {e.ErrorException?.Message}", "Помилка MediaPlayer");
            };
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

                        _player.Open(new Uri(tempFile));
                        _player.Play();
                    }
                    else
                    {
                        string errorMsg = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Помилка сервера ElevenLabs (Код {response.StatusCode}):\n{errorMsg}", "Помилка API");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка мережі: {ex.Message}", "Помилка завантаження");
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
                _voskRecognizer = new VoskRecognizer(model, 16000.0f);

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
                            onCommandRecognized(text);
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