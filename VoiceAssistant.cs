using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Jarvis
{
    public class VoiceAssistant
    {
        private readonly MediaPlayer _player;

        // API
        private readonly string _apiKey = "sk_65a234da2e4e40e4f3cfe0804be4d26bbbbd7d94030c7652";

        // ID voice
        private readonly string _voiceId = "ErXwobaYiN019PkySvjV";

        public VoiceAssistant()
        {
            _player = new MediaPlayer();
            _player.Volume = 1.0;

            // Відстежуємо помилки самого плеєра Windows
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
                    // Авторизація ElevenLabs
                    client.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
                    client.DefaultRequestHeaders.Add("User-Agent", "JarvisApp/1.0");

                    string url = $"https://api.elevenlabs.io/v1/text-to-speech/{_voiceId}";

                    // Формуємо запит.
                    string jsonBody = $@"{{
                        ""text"": ""{text}"",
                        ""model_id"": ""eleven_multilingual_v2"",
                        ""voice_settings"": {{
                            ""stability"": 0.5,
                            ""similarity_boost"": 0.75
                        }}
                    }}";

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Відправляємо запит на сервер
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Зберігаємо отримане аудіо у тимчасовий файл
                        byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                        string tempFile = Path.Combine(Path.GetTempPath(), "jarvis_elevenlabs.mp3");
                        File.WriteAllBytes(tempFile, audioBytes);

                        // Відтворюємо
                        _player.Open(new Uri(tempFile));
                        _player.Play();
                    }
                    else
                    {
                        // Якщо ElevenLabs свариться (наприклад, закінчився ліміт символів)
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
    }
}