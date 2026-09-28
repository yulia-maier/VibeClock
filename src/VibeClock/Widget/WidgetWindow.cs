using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VibeClock.Audio;
using VibeClock.Interaction;
using VibeClock.Rendering;
using VibeClock.Settings;
using VibeClock.Startup;
using VibeClock.Timer;

namespace VibeClock.Widget;

public sealed class WidgetWindow : Window
{
    private readonly CountdownTimer timer = new();
    private readonly DialView dial = new();
    private readonly DialGesture gesture = new();
    private readonly SettingsStore settingsStore = new();
    private readonly WidgetSettings settings;
    private readonly AutostartService autostart = new();
    private readonly FinishSound sound = new();
    private readonly DispatcherTimer refresh;
    private readonly ContextMenu menu;
    private Point pressedAt;
    private bool pressedOnKnob;
    private bool pointerDown;
    private bool selecting;
    private bool keyboardSelecting;

    public WidgetWindow()
    {
        settings = settingsStore.Load();
        Title = "VibeClock";
        Width = Height = 220;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = settings.AlwaysOnTop;
        Content = dial;
        Icon = PixelArt.CreateBody();
        UseLayoutRounding = true;
        menu = CreateMenu();
        refresh = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / 30) };
        refresh.Tick += (_, _) => Refresh();
        timer.Finished += (_, _) => { if (settings.SoundEnabled) sound.Play(); };
        SourceInitialized += (_, _) => WindowPlacement.Restore(this, settings);
        Loaded += (_, _) => { dial.Focus(); Refresh(); };
        Closing += OnClosing;
        dial.MouseLeftButtonDown += OnLeftDown;
        dial.MouseLeftButtonUp += OnLeftUp;
        dial.MouseMove += OnMove;
        dial.LostMouseCapture += (_, _) => { pointerDown = false; selecting = false; Refresh(); };
        dial.MouseRightButtonUp += OnRightUp;
        PreviewKeyDown += OnKeyDown;
    }

    private static double Radius(Point p) => (p - PixelArt.Center).Length;
    private static double Angle(Point p) => DialGesture.Angle(p.X - 128, p.Y - 126);

    private void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        dial.Focus();
        var p = dial.ToArtboard(e.GetPosition(dial));
        e.Handled = true;
        keyboardSelecting = false;
        if (Radius(p) > 96)
        {
            try { DragMove(); }
            catch (InvalidOperationException) { }
            WindowPlacement.Capture(this, settings);
            settingsStore.Save(settings);
            return;
        }
        pointerDown = true;
        pressedAt = p;
        pressedOnKnob = Radius(p) <= 34;
        selecting = !pressedOnKnob;
        if (selecting) { gesture.Begin(Angle(p)); dial.Minutes = gesture.Minutes; }
        dial.CaptureMouse();
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        var p = dial.ToArtboard(e.GetPosition(dial));
        dial.Cursor = Radius(p) > 96 ? Cursors.SizeAll : Cursors.Hand;
        if (!pointerDown) return;
        if (!selecting && (p - pressedAt).Length >= 5 && Radius(p) >= 10)
        {
            selecting = true;
            gesture.Begin(Angle(p));
        }
        if (!selecting || Radius(p) < 10) return;
        gesture.Move(Angle(p));
        dial.Minutes = gesture.Minutes;
    }

    private void OnLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!pointerDown) return;
        e.Handled = true;
        if (selecting) timer.Start(TimeSpan.FromMinutes(gesture.Minutes));
        else if (pressedOnKnob) TogglePause();
        pointerDown = false;
        selecting = false;
        dial.ReleaseMouseCapture();
        Refresh();
    }

    private void OnRightUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        CancelGesture();
        if (Radius(dial.ToArtboard(e.GetPosition(dial))) <= 34) { timer.Reset(); Refresh(); }
        else OpenMenu();
    }

    private void TogglePause()
    {
        if (timer.State == TimerState.Running) timer.Pause();
        else if (timer.State == TimerState.Paused) timer.Resume();
    }

    private void CancelGesture()
    {
        selecting = pointerDown = keyboardSelecting = false;
        if (dial.IsMouseCaptured) dial.ReleaseMouseCapture();
    }

    private void Refresh()
    {
        timer.Update();
        if (!selecting && !keyboardSelecting) dial.Minutes = timer.Remaining.TotalMinutes;
        if (timer.State == TimerState.Running) refresh.Start();
        else refresh.Stop(); // Idle and paused widgets do not continuously repaint.
        Title = timer.State == TimerState.Paused ? "VibeClock — пауза" : "VibeClock";
    }

    private ContextMenu CreateMenu()
    {
        var context = new ContextMenu
        {
            Background = PixelArt.Brush("#FCF7ED"), Foreground = PixelArt.Brush("#624878"),
            BorderBrush = PixelArt.Brush("#AB80C7"), BorderThickness = new Thickness(2),
            Padding = new Thickness(3), FontFamily = new FontFamily("Segoe UI"), FontSize = 12
        };
        var top = new MenuItem { Header = "Always on Top", IsCheckable = true, IsChecked = settings.AlwaysOnTop };
        top.Click += (_, _) => { settings.AlwaysOnTop = Topmost = top.IsChecked; settingsStore.Save(settings); };
        var start = new MenuItem { Header = "Start with Windows", IsCheckable = true };
        start.Click += (_, _) =>
        {
            try { autostart.SetEnabled(start.IsChecked); }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or System.IO.IOException)
            {
                start.IsChecked = !start.IsChecked;
                MessageBox.Show(this, "Windows не разрешила изменить автозапуск для текущего пользователя.", "VibeClock", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        var audio = new MenuItem { Header = "Sound On/Off", IsCheckable = true, IsChecked = settings.SoundEnabled };
        audio.Click += (_, _) => { settings.SoundEnabled = audio.IsChecked; settingsStore.Save(settings); };
        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => Close();
        context.Items.Add(top); context.Items.Add(start); context.Items.Add(audio); context.Items.Add(exit);
        context.Opened += (_, _) =>
        {
            try { start.IsChecked = autostart.IsEnabled; start.IsEnabled = true; }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or System.IO.IOException) { start.IsEnabled = false; }
        };
        return context;
    }

    private void OpenMenu() { menu.PlacementTarget = dial; menu.IsOpen = true; }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // WPF reports F10 as Key.System. Handle its actual key before Windows activates its menu.
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        switch (key)
        {
            case Key.Space: CancelGesture(); TogglePause(); Refresh(); break;
            case Key.Escape: CancelGesture(); timer.Reset(); Refresh(); break;
            case Key.Up:
            case Key.Right:
            case Key.Down:
            case Key.Left:
                if (pointerDown) CancelGesture();
                keyboardSelecting = true;
                dial.Minutes = Math.Clamp(Math.Round(dial.Minutes) + (key is Key.Up or Key.Right ? 1 : -1), 1, 60);
                break;
            case Key.Enter:
                if (keyboardSelecting) { timer.Start(TimeSpan.FromMinutes(dial.Minutes)); keyboardSelecting = false; Refresh(); }
                break;
            case Key.Apps: OpenMenu(); break;
            case Key.F10 when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift): OpenMenu(); break;
            default: return;
        }
        e.Handled = true;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        refresh.Stop();
        WindowPlacement.Capture(this, settings);
        settingsStore.Save(settings);
        menu.IsOpen = false;
        sound.Dispose();
    }
}
