using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Jarvis
{
    public partial class SplashWindow : Window
    {
        private MediaPlayer _backgroundMusic = new MediaPlayer();

        public SplashWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _backgroundMusic.Open(new Uri("ironman_theme.mp3", UriKind.Relative));
                _backgroundMusic.Volume = 0.5; // Громкость от 0.0 до 1.0 (50%)
                _backgroundMusic.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка аудио: " + ex.Message);
            }

            await RunBootSequenceAsync();
        }

        private async Task RunBootSequenceAsync()
        {
            await Task.Delay(1000); 

            AnimateReactor();
            await Task.Delay(1500); 

            string title = "J.A.R.V.I.S"; 
            for (int i = 0; i <= title.Length; i++)
            {
                TitleText.Text = title.Substring(0, i);
                await Task.Delay(150); 
            }
            
            await Task.Delay(500);

            string[] messages = { 
                "> Boot sequence started", 
                "> Neural core online", 
                "> Voice module online", 
                "> Command processor online", 
                "> System ready" 
            };

            foreach (var msg in messages)
            {
                TextBlock tb = new TextBlock 
                { 
                    Text = msg, 
                    Foreground = new SolidColorBrush(Color.FromRgb(41, 255, 230)), 
                    FontFamily = new FontFamily("Consolas"), 
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 0, 10),
                    Opacity = 0.8
                };
                SystemMessagesPanel.Children.Add(tb);
                await Task.Delay(400); 
            }

            await Task.Delay(1200); 

            FinalFlashAndTransition();
        }

        private void AnimateReactor()
        {
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1));
            ReactorCore.BeginAnimation(OpacityProperty, fadeIn);
            ReactorRing.BeginAnimation(OpacityProperty, fadeIn);

            DoubleAnimation pulse = new DoubleAnimation(1, 1.4, TimeSpan.FromSeconds(0.8))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } 
            };
            
            CoreScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            CoreScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        private void FinalFlashAndTransition()
        {
            CoreScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            CoreScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            DoubleAnimation flash = new DoubleAnimation(1.4, 10, TimeSpan.FromSeconds(0.6));
            CoreScale.BeginAnimation(ScaleTransform.ScaleXProperty, flash);
            CoreScale.BeginAnimation(ScaleTransform.ScaleYProperty, flash);

            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.8));
            fadeOut.Completed += (s, e) =>
            {
                _backgroundMusic.Stop();

                MainWindow main = new MainWindow();
                main.Show();
                this.Close(); 
            };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}