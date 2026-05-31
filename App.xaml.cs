using Jarvis;
using System.Windows;

namespace Jarvis
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SplashWindow splash = new SplashWindow();
            splash.Show();
        }
    }
}