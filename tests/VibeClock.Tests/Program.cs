using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VibeClock.Interaction;
using VibeClock.Rendering;
using VibeClock.Settings;
using VibeClock.Timer;

internal static class Program
{
    private static int passed;
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Run("fresh timer is empty", () => { var t = new CountdownTimer(); Equal(TimerState.Idle, t.State); Equal(TimeSpan.Zero, t.Remaining); });
            Run("uses deadline after a stalled UI", () =>
            {
                var clock = new FakeClock(); var t = new CountdownTimer(clock);
                t.Start(TimeSpan.FromMinutes(30)); clock.Advance(TimeSpan.FromMinutes(7.5));
                Equal(TimeSpan.FromMinutes(22.5), t.Remaining);
            });
            Run("pause retains exact fractional remainder", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c);
                t.Start(TimeSpan.FromMinutes(1)); c.Advance(TimeSpan.FromMilliseconds(1250)); t.Pause();
                c.Advance(TimeSpan.FromDays(2)); Equal(TimeSpan.FromMilliseconds(58750), t.Remaining);
                t.Resume(); c.Advance(TimeSpan.FromMilliseconds(750)); Equal(TimeSpan.FromSeconds(58), t.Remaining);
            });
            Run("completion fires once even after sleep", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c); int finished = 0;
                t.Finished += (_, _) => finished++;
                t.Start(TimeSpan.FromMinutes(1)); c.Advance(TimeSpan.FromHours(8));
                for (int i = 0; i < 10; i++) t.Update();
                Equal(1, finished); Equal(TimerState.Idle, t.State); Equal(TimeSpan.Zero, t.Remaining);
                t.Start(TimeSpan.FromMinutes(1)); c.Advance(TimeSpan.FromMinutes(1)); t.Update(); Equal(2, finished);
            });
            Run("reset never rings", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c); int finished = 0; t.Finished += (_, _) => finished++;
                t.Start(TimeSpan.FromMinutes(1)); t.Reset(); c.Advance(TimeSpan.FromHours(1)); t.Update(); Equal(0, finished);
            });
            Run("pause at deadline completes rather than freezing zero", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c); int finished = 0; t.Finished += (_, _) => finished++;
                t.Start(TimeSpan.FromMinutes(1)); c.Advance(TimeSpan.FromMinutes(1)); t.Pause(); t.Resume(); Equal(TimerState.Idle, t.State); Equal(1, finished);
            });
            Run("new selection replaces the previous deadline", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c); t.Start(TimeSpan.FromMinutes(30));
                c.Advance(TimeSpan.FromMinutes(10)); t.Start(TimeSpan.FromMinutes(5)); Equal(TimeSpan.FromMinutes(5), t.Remaining);
            });
            Run("only positive durations through 60 minutes", () =>
            {
                var t = new CountdownTimer(); Throws<ArgumentOutOfRangeException>(() => t.Start(TimeSpan.Zero));
                Throws<ArgumentOutOfRangeException>(() => t.Start(TimeSpan.FromMinutes(61)));
                Throws<ArgumentOutOfRangeException>(() => t.Start(TimeSpan.FromSeconds(-1)));
                t.Start(TimeSpan.FromHours(1)); Equal(TimerState.Running, t.State);
            });
            Run("clock correction cannot overfill the dial", () =>
            {
                var c = new FakeClock(); var t = new CountdownTimer(c); t.Start(TimeSpan.FromHours(1));
                c.Advance(TimeSpan.FromHours(-1)); Equal(TimeSpan.FromHours(1), t.Remaining);
            });
            Run("dial cardinals match labels", () =>
            {
                Equal(0.0, DialGesture.Angle(0, -10)); Equal(90.0, DialGesture.Angle(10, 0));
                Equal(180.0, DialGesture.Angle(0, 10)); Equal(270.0, DialGesture.Angle(-10, 0));
                foreach (int minutes in new[] { 15, 30, 45, 60 }) { var g = new DialGesture(); g.Begin(minutes * 6); Equal(minutes, g.Minutes); }
            });
            Run("crossing 60 clockwise clamps without jumping to 1", () =>
            {
                var g = new DialGesture(); g.Begin(348); g.Move(354); g.Move(0); g.Move(6); Equal(60, g.Minutes);
                g.Move(0); Equal(59, g.Minutes);
            });
            Run("crossing minimum counterclockwise does not jump to 60", () =>
            {
                var g = new DialGesture(); g.Begin(12); g.Move(6); g.Move(0); g.Move(354); Equal(1, g.Minutes);
            });
            Run("full circle is selectable at noon", () => { var g = new DialGesture(); g.Begin(0); Equal(60, g.Minutes); g.Move(354); Equal(59, g.Minutes); });
            Run("settings round trip and recover from corruption", () =>
            {
                string path = Path.Combine(Path.GetTempPath(), "VibeClock-tests-" + Guid.NewGuid() + ".json");
                try
                {
                    var store = new SettingsStore(path); Equal(true, store.Load().SoundEnabled);
                    Equal(true, store.Save(new WidgetSettings { Left = -700, Top = 120, AlwaysOnTop = true, SoundEnabled = false }));
                    var loaded = store.Load(); Equal(-700.0, loaded.Left!.Value); Equal(true, loaded.AlwaysOnTop); Equal(false, loaded.SoundEnabled);
                    File.WriteAllText(path, "not json"); Equal(true, store.Load().SoundEnabled);
                }
                finally { File.Delete(path); }
            });
            Run("sector area and direction", () =>
            {
                Equal(false, RainbowProgress.Sector(0).FillContains(new Point(150, 100)));
                Equal(true, RainbowProgress.Sector(15).FillContains(new Point(160, 100)));
                Equal(false, RainbowProgress.Sector(15).FillContains(new Point(160, 150)));
                Equal(true, RainbowProgress.Sector(30).FillContains(new Point(160, 150)));
                Equal(false, RainbowProgress.Sector(30).FillContains(new Point(100, 150)));
                Equal(true, RainbowProgress.Sector(60).FillContains(new Point(100, 150)));
            });
            Run("rainbow stays fixed as sector shrinks", () =>
            {
                var full = Render(60, 256); var half = Render(30, 256); var quarter = Render(15, 256);
                Equal(Pixel(full, 176, 102), Pixel(half, 176, 102));
                Equal(Pixel(full, 176, 102), Pixel(quarter, 176, 102));
                if (Pixel(full, 78, 126) == Pixel(half, 78, 126)) throw new Exception("Left half did not disappear.");
            });
            string output = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Environment.CurrentDirectory, "artifacts", "previews");
            Directory.CreateDirectory(output);
            foreach (int minutes in new[] { 0, 15, 30, 60 })
            {
                Save(Render(minutes, 220), Path.Combine(output, $"timer-{minutes:00}.png"));
                Save(Render(minutes, 660), Path.Combine(output, $"timer-{minutes:00}-large.png"));
            }
            Console.WriteLine($"PASS: {passed} checks. WPF previews: {output}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static BitmapSource Render(double minutes, int size)
    {
        var view = new DialView { Width = size, Height = size, Minutes = minutes };
        view.Measure(new Size(size, size)); view.Arrange(new Rect(0, 0, size, size)); view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view); return bitmap;
    }
    private static uint Pixel(BitmapSource image, int x, int y) { var pixel = new uint[1]; image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0); return pixel[0]; }
    private static void Save(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path); encoder.Save(file);
    }
    private static void Run(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
}
