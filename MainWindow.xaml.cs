using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Jarvis
{
    public partial class MainWindow : Window
    {
        private VoiceAssistant _voice;
        private bool isListening = false;

        public MainWindow()
        {
            InitializeComponent();
            _voice = new VoiceAssistant();
            Opacity = 0;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();
            StartMicAnimation();
            StartCoreAnimation();

            await Task.Delay(1000);
            await _voice.GreetUserAsync();
        }

        // ========================================================================
        // АНІМАЦІЇ ІНТЕРФЕЙСУ
        // ========================================================================

        private void StartFadeIn()
        {
            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1)
            };
            BeginAnimation(Window.OpacityProperty, fade);
        }

        private void StartMicAnimation()
        {
            ScaleTransform scale = new ScaleTransform(1, 1);
            MicButton.RenderTransform = scale;
            MicButton.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation pulse = new DoubleAnimation
            {
                From = 1,
                To = 1.15,
                Duration = TimeSpan.FromMilliseconds(800),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        private void StartCoreAnimation()
        {
            RotateTransform rotate = new RotateTransform();
            OuterRing.RenderTransform = rotate;
            OuterRing.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation rotation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(15),
                RepeatBehavior = RepeatBehavior.Forever
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, rotation);
        }

        // ========================================================================
        // УПРАВЛІННЯ ВІКНОМ (Перетягування та кнопки)
        // ========================================================================

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ========================================================================
        // ЛОГІКА ТЕКСТОВОГО ЧАТУ
        // ========================================================================

        private static string GetConfig(string key)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
            {
                MessageBox.Show("Файл appsettings.json не знайдено!\nСкопіюй appsettings.example.json і заповни ключі.");
                return "";
            }
            var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return json.RootElement.GetProperty(key).GetString() ?? "";
        }

        private async Task<string> SendToAI(string prompt)
        {
            try
            {
                string apiKey = GetConfig("GroqApiKey");
                string url = "https://api.groq.com/openai/v1/chat/completions";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    var requestData = new
                    {
                        model = "llama-3.3-70b-versatile",
                        max_tokens = 1024,
                        messages = new[]
                        {
                    new { role = "system", content = "Ти голосовий асистент JARVIS. Відповідай коротко і по справі українською мовою." },
                    new { role = "user", content = prompt }
                }
                    };

                    string jsonBody = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (JsonDocument doc = JsonDocument.Parse(responseString))
                        {
                            return doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();
                        }
                    }
                    else
                    {
                        return $"Помилка API ({response.StatusCode}): {responseString}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Критична помилка: {ex.Message}";
            }
        }

        private void ChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            VoiceCorePanel.Visibility = Visibility.Collapsed;
            TextChatPanel.Visibility = Visibility.Visible;
        }

        private void ChatInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ChatInput.Text))
                ChatPlaceholder.Visibility = Visibility.Visible;
            else
                ChatPlaceholder.Visibility = Visibility.Hidden;
        }

        // Добавили слово async
        private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = ChatInput.Text.Trim();

                if (!string.IsNullOrEmpty(command))
                {
                    AddChatMessage(command, true);
                    ChatInput.Text = "";

                    AiStateText.Text = "Обробка...";
                    AiSubStateText.Text = command;

                    // 1. Получаем ответ от ИИ
                    string aiResponse = await SendToAI(command);
                    AddChatMessage(aiResponse, false);

                    // 2. Включаем озвучку через ElevenLabs
                    // Мы не ставим await перед SpeakAsync, чтобы озвучка шла фоном 
                    // и не блокировала интерфейс, пока она играет
                    _ = _voice.SpeakAsync(aiResponse);

                    AiStateText.Text = "Очікування";
                    AiSubStateText.Text = "Готовий до нових команд";
                }
            }
        }

        private void AddChatMessage(string text, bool isUser)
        {
            Border bubble = new Border
            {
                CornerRadius = isUser ? new CornerRadius(15, 15, 0, 15) : new CornerRadius(15, 15, 15, 0),
                Background = isUser ? new SolidColorBrush(Color.FromRgb(0, 97, 255)) : new SolidColorBrush(Color.FromRgb(26, 36, 56)),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = isUser ? new Thickness(50, 0, 0, 15) : new Thickness(0, 0, 50, 15),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            TextBlock messageText = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = messageText;
            ChatHistoryPanel.Children.Add(bubble);
            ChatScroll.ScrollToBottom();
        }

        // ========================================================================
        // КНОПКА МІКРОФОНА (Перемикання на Голос)
        // ========================================================================

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            TextChatPanel.Visibility = Visibility.Collapsed;
            VoiceCorePanel.Visibility = Visibility.Visible;
            Keyboard.ClearFocus();

            isListening = !isListening;
            if (isListening)
            {
                AiStateText.Text = "Слухаю...";
                AiSubStateText.Text = "Говоріть зараз...";
                OuterRing.Stroke = new SolidColorBrush(Color.FromRgb(255, 69, 0));
            }
            else
            {
                AiStateText.Text = "Очікування";
                AiSubStateText.Text = "Натисніть мікрофон для старту";
                OuterRing.Stroke = new SolidColorBrush(Color.FromRgb(0, 97, 255));
            }
        }

        private async Task EnsureModelDownloaded()
        {
            string modelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VASYA");
            string checkFile = Path.Combine(modelDir, "am", "final.mdl");

            if (File.Exists(checkFile) && new FileInfo(checkFile).Length > 1024 * 1024)
                return;

            AiStateText.Text = "Завантаження моделі...";
            AiSubStateText.Text = "Перший запуск, зачекайте (~1 GB)";

            var files = new Dictionary<string, string>
            {
                ["am/final.mdl"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/am/final.mdl",
                ["conf/mfcc.conf"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/conf/mfcc.conf",
                ["conf/model.conf"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/conf/model.conf",
                ["graph/HCLG.fst"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/graph/HCLG.fst",
                ["graph/words.txt"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/graph/words.txt",
                ["graph/phones/word_boundary.int"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/graph/phones/word_boundary.int",
                ["ivector/final.dubm"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/final.dubm",
                ["ivector/final.ie"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/final.ie",
                ["ivector/final.mat"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/final.mat",
                ["ivector/global_cmvn.stats"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/global_cmvn.stats",
                ["ivector/online_cmvn.conf"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/online_cmvn.conf",
                ["ivector/splice.conf"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/splice.conf",
                ["ivector/splice_opts"] = "https://huggingface.co/Zumich312/VASYA/resolve/main/ivector/splice_opts",
            };

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(60);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                int current = 0;
                int total = files.Count;

                foreach (var file in files)
                {
                    current++;
                    string localPath = System.IO.Path.Combine(modelDir, file.Key.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localPath));

                    if (File.Exists(localPath) && new FileInfo(localPath).Length > 1024)
                        continue;

                    AiStateText.Text = $"Файл {current}/{total}";
                    AiSubStateText.Text = file.Key;

                    try
                    {
                        var response = await client.GetAsync(file.Value, HttpCompletionOption.ResponseHeadersRead);
                        long? totalBytes = response.Content.Headers.ContentLength;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(localPath))
                        {
                            byte[] buffer = new byte[81920];
                            long downloaded = 0;
                            int bytesRead;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloaded += bytesRead;

                                if (totalBytes.HasValue)
                                {
                                    int percent = (int)(downloaded * 100 / totalBytes.Value);
                                    AiStateText.Text = $"Файл {current}/{total}: {percent}%";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка завантаження {file.Key}:\n{ex.Message}");
                    }
                }
            }

            AiStateText.Text = "Очікування";
            AiSubStateText.Text = "Готовий до нових команд";
        }

        // ========================================================================
        // ШВИДКІ ДІЇ ТА NATIVE METHODS
        // ========================================================================

        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        }

        private void BtnOpenNotepad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var existing = System.Diagnostics.Process.GetProcessesByName("notepad");
                if (existing.Length > 0)
                {
                    var process = existing[0];
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    System.Threading.Thread.Sleep(200);

                    NativeMethods.keybd_event(0x11, 0, 0, 0); // Ctrl down
                    NativeMethods.keybd_event(0x4E, 0, 0, 0); // N down
                    NativeMethods.keybd_event(0x4E, 0, 2, 0); // N up
                    NativeMethods.keybd_event(0x11, 0, 2, 0); // Ctrl up
                }
                else
                {
                    System.Diagnostics.Process.Start("notepad.exe");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenCalc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("calc.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося відкрити калькулятор: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                System.Diagnostics.Process.Start(chromePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося відкрити браузер: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
        }
    }
}