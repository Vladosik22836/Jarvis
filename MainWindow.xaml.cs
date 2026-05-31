using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Jarvis
{
    /// <summary>
    /// Головне вікно застосунку J.A.R.V.I.S
    /// Тут ініціалізується UI та запускаються базові анімації інтерфейсу
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); // Підключення XAML-інтерфейсу

            _voice = new VoiceAssistant();

            Opacity = 0; // Початково вікно повністю прозоре (для ефекту появи)

            Loaded += MainWindow_Loaded;// Подія: коли вікно повністю завантажилось — запускаємо логіку

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm");
                DateText.Text = DateTime.Now.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("uk-UA"));
            };
            timer.Start();
        }

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

        /// <summary>
        /// Подія завантаження вікна
        /// Тут стартують всі основні анімації інтерфейсу
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();       // Плавна поява вікна
            StartMicAnimation(); // Анімація кнопки мікрофона (пульсація)
            StartCoreAnimation(); // Обертання AI-ядра (зовнішнє кільце)

            AddLog("🛡️", "Програма запущена", "J.A.R.V.I.S активний", "#00FF88"); // ← додай

            await Task.Delay(1000);

            await _voice.GreetUserAsync();
        }

        /// <summary>
        /// Анімація появи вікна (fade-in ефект)
        /// Створює відчуття "запуску системи Jarvis"
        /// </summary>
        private void StartFadeIn()
        {
            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0,   // початкова прозорість
                To = 1,     // повна видимість
                Duration = TimeSpan.FromSeconds(1) // час появи
            };

            // Запуск анімації прозорості вікна
            BeginAnimation(Window.OpacityProperty, fade);
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // ========================================================================
        // УПРАВЛІННЯ ВІКНОМ (Перетягування та кнопки управління)
        // ========================================================================

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ========================================================================
        // ПУСТЫЕ ЗАГЛУШКИ ДЛЯ ФРОНТЕНДА (Бэкендер заполнит их позже)
        // ========================================================================

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) { }

        private void MicButton_Click(object sender, RoutedEventArgs e) { }

        /// <summary>
        /// Анімація кнопки мікрофона
        /// Ефект "пульсації" показує, що система слухає користувача
        /// </summary>
        private void StartMicAnimation()
        {
            // Масштабування елемента (збільшення/зменшення)
            ScaleTransform scale = new ScaleTransform(1, 1);

            // Прив'язка трансформації до кнопки MicButton з XAML
            MicButton.RenderTransform = scale;

            // Центр масштабування (щоб пульсація йшла від центру кнопки)
            MicButton.RenderTransformOrigin = new Point(0.5, 0.5);

            // Анімація пульсації (збільшення кнопки)
            DoubleAnimation pulse = new DoubleAnimation
            {
                From = 1,                 // нормальний розмір
                To = 1.15,               // збільшення на 15%
                Duration = TimeSpan.FromMilliseconds(800), // швидкість пульсації
                AutoReverse = true,      // повернення назад
                RepeatBehavior = RepeatBehavior.Forever // нескінченний цикл
            };

            // Анімація по осі X (ширина)
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);

            // Анімація по осі Y (висота)
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        /// <summary>
        /// Анімація центрального AI-ядра (OuterRing)
        /// Створює ефект "живої системи" через постійне обертання
        /// </summary>
        private void StartCoreAnimation()
        {
            // Об'єкт для обертання елемента
            RotateTransform rotate = new RotateTransform();

            // Прив'язка обертання до зовнішнього кільця AI
            OuterRing.RenderTransform = rotate;

            // Центр обертання (середина кола)
            OuterRing.RenderTransformOrigin = new Point(0.5, 0.5);

            // Анімація обертання 0° → 360°
            DoubleAnimation rotation = new DoubleAnimation
            {
                From = 0,                  // стартовий кут
                To = 360,                 // повний оберт
                Duration = TimeSpan.FromSeconds(15), // швидкість обертання
                RepeatBehavior = RepeatBehavior.Forever // безкінечний цикл
            };

            // Запуск анімації обертання
            rotate.BeginAnimation(RotateTransform.AngleProperty, rotation);
        }

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
            var existing = System.Diagnostics.Process.GetProcessesByName("CalculatorApp");
            if (existing.Length > 0)
            {
                existing[0].Kill();
                System.Threading.Thread.Sleep(300);
                System.Diagnostics.Process.Start("calc.exe");
                AddLog("🧮", "Калькулятор перезапущено", "Restart Calculator", "#00C8FF");
            }
            else
            {
                System.Diagnostics.Process.Start("calc.exe");
                AddLog("🧮", "Калькулятор відкрито", "Запуск Calculator", "#00C8FF");
            }
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

        private VoiceAssistant _voice;
    }
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }


}