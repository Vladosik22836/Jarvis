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

            Loaded += MainWindow_Loaded;
            // Подія: коли вікно повністю завантажилось — запускаємо логіку
        }

        /// <summary>
        /// Подія завантаження вікна
        /// Тут стартують всі основні анімації інтерфейсу
        /// </summary>
        private async   void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();       // Плавна поява вікна
            StartMicAnimation(); // Анімація кнопки мікрофона (пульсація)
            StartCoreAnimation(); // Обертання AI-ядра (зовнішнє кільце)

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

        private void BtnOpenNotepad_Click(object sender, RoutedEventArgs e) { }

        private void BtnOpenCalc_Click(object sender, RoutedEventArgs e) { }

        private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e) { }

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

        private VoiceAssistant _voice;
    }
}