namespace VibeClock.Interaction;

/// <summary>Unwraps clockwise angles across twelve o'clock without jumping from 60 to 1.</summary>
public sealed class DialGesture
{
    private double lastAngle;
    private double accumulated;

    public int Minutes => Math.Clamp((int)Math.Round(accumulated / 6, MidpointRounding.AwayFromZero), 1, 60);

    public void Begin(double clockwiseAngle, double? initialMinutes = null)
    {
        lastAngle = Normalize(clockwiseAngle);
        accumulated = initialMinutes.HasValue ? Math.Clamp(initialMinutes.Value, 0, 60) * 6 : lastAngle;
        if (initialMinutes is null && accumulated < 0.5) accumulated = 360;
    }

    public void Move(double clockwiseAngle)
    {
        double angle = Normalize(clockwiseAngle);
        double delta = angle - lastAngle;
        if (delta > 180) delta -= 360;
        if (delta < -180) delta += 360;
        accumulated = Math.Clamp(accumulated + delta, 0, 360);
        lastAngle = angle;
    }

    public static double Angle(double x, double y) => Normalize(Math.Atan2(x, -y) * 180 / Math.PI);
    private static double Normalize(double angle) => (angle % 360 + 360) % 360;
}
