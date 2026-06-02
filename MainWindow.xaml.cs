using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
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

        private System.Diagnostics.PerformanceCounter _cpuCounter =
            new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
        private System.Diagnostics.PerformanceCounter _ramCounter =
            new System.Diagnostics.PerformanceCounter("Memory", "% Committed Bytes In Use");
        private List<float> _cpuHistory = new List<float>();

        public MainWindow()
        {
            InitializeComponent();
            _voice = new VoiceAssistant();
            Opacity = 0;
            Loaded += MainWindow_Loaded;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm");
                DateText.Text = DateTime.Now.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("uk-UA"));

                _cpuHistory.Add(_cpuCounter.NextValue());
                if (_cpuHistory.Count > 5) _cpuHistory.RemoveAt(0);
                CpuText.Text = $"{(int)_cpuHistory.Average()}%";
                RamText.Text = $"{(int)_ramCounter.NextValue()}%";
            };
            timer.Start();

            bool notepadWasOpen = false;
            bool calcWasOpen = false;
            bool chromeWasOpen = false;

            var processTimer = new System.Windows.Threading.DispatcherTimer();
            processTimer.Interval = TimeSpan.FromSeconds(2);
            processTimer.Tick += (s, e) =>
            {
                bool notepadOpen = System.Diagnostics.Process.GetProcessesByName("notepad").Length > 0;
                bool calcOpen = System.Diagnostics.Process.GetProcessesByName("CalculatorApp").Length > 0;
                bool chromeOpen = System.Diagnostics.Process.GetProcessesByName("chrome").Length > 0;

                if (notepadWasOpen && !notepadOpen)
                    AddLog("📝", "Блокнот закрито", "Notepad завершено", "#FF6B6B");
                if (calcWasOpen && !calcOpen)
                    AddLog("🧮", "Калькулятор закрито", "Calculator завершено", "#FF6B6B");
                if (chromeWasOpen && !chromeOpen)
                    AddLog("🌐", "Браузер закрито", "Chrome завершено", "#FF6B6B");

                notepadWasOpen = notepadOpen;
                calcWasOpen = calcOpen;
                chromeWasOpen = chromeOpen;
            };
            processTimer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();
            StartMicAnimation();
            StartCoreAnimation();

            // Спочатку завантажуємо модель
            await EnsureModelDownloaded();

            // Тільки після завантаження ініціалізуємо Vosk
            _voice.InitSpeech(async (text) =>
            {
                if (text.Contains("джарвіс") || text.Contains("jarvis"))
                {
                    string command = text
                        .Replace("джарвіс", "")
                        .Replace("jarvis", "")
                        .Trim();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        AiStateText.Text = "Слухаю команду...";
                        AddLog("🎙️", "Активовано голосом", $"Команда: {command}", "#00C8FF");
                    });

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (string.IsNullOrWhiteSpace(command))
                            await _voice.SpeakAsync("Слухаю вас, сер.");
                        else if (command.Contains("привіт"))
                            await _voice.SpeakAsync("Привіт, сер. Чим можу допомогти?");
                        else if (command.Contains("час") || command.Contains("година"))
                        {
                            string time = DateTime.Now.ToString("HH:mm");
                            await _voice.SpeakAsync($"Зараз {time}, сер.");
                        }
                        else if (command.Contains("блокнот"))
                        {
                            System.Diagnostics.Process.Start("notepad.exe");
                            await _voice.SpeakAsync("Відкриваю блокнот, сер.");
                        }
                        else if (command.Contains("калькулятор"))
                        {
                            System.Diagnostics.Process.Start("calc.exe");
                            await _voice.SpeakAsync("Відкриваю калькулятор, сер.");
                        }
                        else if (command.Contains("браузер") || command.Contains("хром"))
                        {
                            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                            System.Diagnostics.Process.Start(chromePath);
                            await _voice.SpeakAsync("Відкриваю браузер, сер.");
                        }
                        else
                            await _voice.SpeakAsync($"Вибачте, команду {command} не розпізнано.");
                    });
                }
            });

            AddLog("🛡️", "Програма запущена", "J.A.R.V.I.S активний", "#00FF88");

            await Task.Delay(1000);
            await _voice.GreetUserAsync();
        }

        // ========================================================================
        // ЗАВАНТАЖЕННЯ МОДЕЛІ
        // ========================================================================

        private async Task EnsureModelDownloaded()
        {
            // HEAD version: downloads from R2 CDN into "model" folder
            string modelDirHead = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model");
            string checkFileHead = System.IO.Path.Combine(modelDirHead, "am", "final.mdl");

            bool headReady = File.Exists(checkFileHead) && new FileInfo(checkFileHead).Length > 1024 * 1024;

            AiStateText.Text = "Завантаження моделі...";
            AiSubStateText.Text = "Перший запуск, зачекайте (~1 GB)";

            // ---- HEAD sources (R2 CDN) ----
            string baseUrl = "https://pub-891b2194b1004c19bbc501b7262a6c22.r2.dev";

            var filesHead = new Dictionary<string, string>
            {
                ["am/final.mdl"] = $"{baseUrl}/am/final.mdl",
                ["conf/mfcc.conf"] = $"{baseUrl}/conf/mfcc.conf",
                ["conf/model.conf"] = $"{baseUrl}/conf/model.conf",
                ["graph/HCLG.fst"] = $"{baseUrl}/graph/HCLG.fst",
                ["graph/words.txt"] = $"{baseUrl}/graph/words.txt",
                ["graph/phones/word_boundary.int"] = $"{baseUrl}/graph/phones/word_boundary.int",
                ["ivector/final.dubm"] = $"{baseUrl}/ivector/final.dubm",
                ["ivector/final.ie"] = $"{baseUrl}/ivector/final.ie",
                ["ivector/final.mat"] = $"{baseUrl}/ivector/final.mat",
                ["ivector/global_cmvn.stats"] = $"{baseUrl}/ivector/global_cmvn.stats",
                ["ivector/online_cmvn.conf"] = $"{baseUrl}/ivector/online_cmvn.conf",
                ["ivector/splice.conf"] = $"{baseUrl}/ivector/splice.conf",
                ["ivector/splice_opts"] = $"{baseUrl}/ivector/splice_opts",
            };

            var minSizes = new Dictionary<string, long>
            {
                ["am/final.mdl"] = 20_000_000,
                ["conf/mfcc.conf"] = 100,
                ["conf/model.conf"] = 100,
                ["graph/HCLG.fst"] = 800_000_000,
                ["graph/words.txt"] = 100_000_000,
                ["graph/phones/word_boundary.int"] = 1_000,
                ["ivector/final.dubm"] = 100_000,
                ["ivector/final.ie"] = 10_000_000,
                ["ivector/final.mat"] = 10_000,
                ["ivector/global_cmvn.stats"] = 100,
                ["ivector/online_cmvn.conf"] = 10,
                ["ivector/splice.conf"] = 10,
                ["ivector/splice_opts"] = 10,
            };

            // ---- front sources (HuggingFace / VASYA) ----
            var filesFront = new Dictionary<string, string>
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
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                // Download HEAD model (R2)
                if (!headReady)
                {
                    int current = 0;
                    int total = filesHead.Count;
                    foreach (var file in filesHead)
                    {
                        current++;
                        string localPath = System.IO.Path.Combine(modelDirHead, file.Key.Replace('/', System.IO.Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localPath));

                        long minSize = minSizes.ContainsKey(file.Key) ? minSizes[file.Key] : 10;
                        if (File.Exists(localPath) && new FileInfo(localPath).Length >= minSize)
                            continue;

                        AiStateText.Text = $"[model] Файл {current}/{total}";
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
                                        AiStateText.Text = $"[model] Файл {current}/{total}: {percent}%";
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
            }

            AiStateText.Text = "Очікування";
            AiSubStateText.Text = "Готовий до нових команд";
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
        // ЖУРНАЛ ПОДІЙ
        // ========================================================================

        private void AddLog(string icon, string title, string subtitle, string iconColor)
        {
            var item = new
            {
                Icon = icon,
                Title = title,
                Subtitle = subtitle,
                IconColor = iconColor,
                Time = DateTime.Now.ToString("HH:mm")
            };
            LogListBox.Items.Insert(0, item);
        }

        private void LogListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LogListBox.SelectedItem != null)
                LogListBox.Items.Remove(LogListBox.SelectedItem);
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

                    string aiResponse = await SendToAI(command);
                    AddChatMessage(aiResponse, false);

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
        // КНОПКА МІКРОФОНА
        // ========================================================================

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            // front: toggle voice panel visibility
            TextChatPanel.Visibility = Visibility.Collapsed;
            VoiceCorePanel.Visibility = Visibility.Visible;
            Keyboard.ClearFocus();

            // HEAD: toggle VoiceAssistant listening state
            if (_voice.IsListening)
            {
                _voice.StopListening();
                isListening = false;
                AiStateText.Text = "Мікрофон вимкнено";
                AiSubStateText.Text = "Натисніть мікрофон для старту";
                OuterRing.Stroke = new SolidColorBrush(Color.FromRgb(0, 97, 255));
                AddLog("🎙️", "Мікрофон вимкнено", "Очікування команди", "#FF6B6B");
            }
            else
            {
                _voice.StartListening();
                isListening = true;
                AiStateText.Text = "Слухаю...";
                AiSubStateText.Text = "Говоріть зараз...";
                OuterRing.Stroke = new SolidColorBrush(Color.FromRgb(255, 69, 0));
                AddLog("🎙️", "Мікрофон увімкнено", "Очікування слова Jarvis", "#00FF88");
            }
        }

        // ========================================================================
        // ШВИДКІ ДІЇ
        // ========================================================================

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
                    AddLog("📝", "Блокнот відкрито", "Запуск Notepad", "#00C8FF");
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
                AddLog("🧮", "Калькулятор відкрито", "Запуск Calculator", "#00C8FF");
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
                var existing = System.Diagnostics.Process.GetProcessesByName("chrome");
                if (existing.Length > 0)
                    NativeMethods.SetForegroundWindow(existing[0].MainWindowHandle);
                else
                {
                    string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                    System.Diagnostics.Process.Start(chromePath);
                    AddLog("🌐", "Браузер відкрито", "Запуск Chrome", "#00C8FF");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося відкрити браузер: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            AddLog("🗑️", "Журнал очищено", "Всі записи видалено", "#FF6B6B");
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    }
}