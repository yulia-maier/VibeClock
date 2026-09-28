using System.Windows;
using VibeClock.Widget;

namespace VibeClock;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new WidgetWindow();
        MainWindow.Show();
    }
}
