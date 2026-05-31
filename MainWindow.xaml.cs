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

            Opacity = 0; // Початково вікно повністю прозоре (для ефекту появи)

            Loaded += MainWindow_Loaded;
            // Подія: коли вікно повністю завантажилось — запускаємо логіку
        }

        /// <summary>
        /// Подія завантаження вікна
        /// Тут стартують всі основні анімації інтерфейсу
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();       // Плавна поява вікна
            StartMicAnimation(); // Анімація кнопки мікрофона (пульсація)
            StartCoreAnimation(); // Обертання AI-ядра (зовнішнє кільце)
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

        private void OpenNotepad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var existing = System.Diagnostics.Process.GetProcessesByName("notepad");

                if (existing.Length > 0)
                {
                    var process = existing[0];
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);

                    // Чекаємо щоб вікно стало активним
                    System.Threading.Thread.Sleep(200);

                    // Ctrl затиснути
                    NativeMethods.keybd_event(0x11, 0, 0, 0); // VK_CONTROL down
                                                              // N натиснути
                    NativeMethods.keybd_event(0x4E, 0, 0, 0); // N down
                    NativeMethods.keybd_event(0x4E, 0, 2, 0); // N up
                                                              // Ctrl відпустити
                    NativeMethods.keybd_event(0x11, 0, 2, 0); // VK_CONTROL up
                }
                else
                {
                    System.Diagnostics.Process.Start("notepad.exe");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("calc.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося відкрити калькулятор: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                System.Diagnostics.Process.Start(chromePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося відкрити браузер: {ex.Message}",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    }
}