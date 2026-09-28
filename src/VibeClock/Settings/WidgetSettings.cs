namespace VibeClock.Settings;

public sealed class WidgetSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool SoundEnabled { get; set; } = true;
}
