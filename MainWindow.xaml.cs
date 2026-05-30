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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Opacity = 0;

            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartFadeIn();
            StartMicAnimation();
            StartCoreAnimation();
        }
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
    }
}