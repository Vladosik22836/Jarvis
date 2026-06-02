using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

            // Спочатку качаємо модель
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
            string modelDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model");
            string checkFile = System.IO.Path.Combine(modelDir, "am", "final.mdl");

            if (File.Exists(checkFile) && new FileInfo(checkFile).Length > 1024 * 1024)
                return;

            AiStateText.Text = "Завантаження моделі...";
            AiSubStateText.Text = "Перший запуск, зачекайте (~1 GB)";

            string baseUrl = "https://pub-891b2194b1004c19bbc501b7262a6c22.r2.dev";

            var files = new Dictionary<string, string>
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

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                int current = 0;
                int total = files.Count;

                foreach (var file in files)
                {
                    current++;
                    string localPath = System.IO.Path.Combine(modelDir, file.Key.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localPath));

                    long minSize = minSizes.ContainsKey(file.Key) ? minSizes[file.Key] : 10;
                    if (File.Exists(localPath) && new FileInfo(localPath).Length >= minSize)
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
        // АНІМАЦІЇ
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
        // УПРАВЛІННЯ ВІКНОМ
        // ========================================================================

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

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
        // ШВИДКІ ДІЇ
        // ========================================================================

        private void BtnOpenNotepad_Click(object sender, RoutedEventArgs e)
        {
            var existing = System.Diagnostics.Process.GetProcessesByName("notepad");
            if (existing.Length > 0)
                NativeMethods.SetForegroundWindow(existing[0].MainWindowHandle);
            else
            {
                System.Diagnostics.Process.Start("notepad.exe");
                AddLog("📝", "Блокнот відкрито", "Запуск Notepad", "#00C8FF");
            }
        }

        private void BtnOpenCalc_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("calc.exe");
            AddLog("🧮", "Калькулятор відкрито", "Запуск Calculator", "#00C8FF");
        }

        private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
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

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            AddLog("🗑️", "Журнал очищено", "Всі записи видалено", "#FF6B6B");
        }

        // ========================================================================
        // КНОПКА МІКРОФОНА
        // ========================================================================

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_voice.IsListening)
            {
                _voice.StopListening();
                AiStateText.Text = "Мікрофон вимкнено";
                AddLog("🎙️", "Мікрофон вимкнено", "Очікування команди", "#FF6B6B");
            }
            else
            {
                _voice.StartListening();
                AiStateText.Text = "Слухаю...";
                AddLog("🎙️", "Мікрофон увімкнено", "Очікування слова Jarvis", "#00FF88");
            }
        }

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
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}