using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VibeClock.Rendering;

public static class RainbowProgress
{
    private static readonly (double Position, Color Color)[] Stops =
    [
        (0, PixelArt.Color("#FF8779")), (0.08, PixelArt.Color("#FFAE57")),
        (0.17, PixelArt.Color("#F8E76A")), (0.25, PixelArt.Color("#A5DF63")),
        (0.34, PixelArt.Color("#64D9A5")), (0.43, PixelArt.Color("#53D6D1")),
        (0.50, PixelArt.Color("#4BC8EB")), (0.60, PixelArt.Color("#559EFA")),
        (0.70, PixelArt.Color("#827CFA")), (0.79, PixelArt.Color("#BC74ED")),
        (0.89, PixelArt.Color("#F279CF")), (1, PixelArt.Color("#FF699B"))
    ];

    // The angular rainbow is calculated only once. Changing time changes the clip, never the colors.
    public static BitmapSource CreateTexture()
    {
        const int size = 128;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - 64, dy = y + 0.5 - 64;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r > 64 || r < 27) continue;
                double angle = (Math.Atan2(dx, -dy) / (2 * Math.PI) + 1) % 1;
                Color color = Sample(angle);
                double light = r < 36 ? 0.20 : r < 46 ? 0.27 : r < 56 ? 0.13 : 0;
                int p = (y * size + x) * 4;
                pixels[p] = Mix(color.B, light); pixels[p + 1] = Mix(color.G, light); pixels[p + 2] = Mix(color.R, light); pixels[p + 3] = 255;
            }
        var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, pixels, size * 4);
        bitmap.Freeze(); return bitmap;
    }

    private static byte Mix(byte channel, double white) => (byte)(channel + (255 - channel) * white);
    private static Color Sample(double position)
    {
        for (int i = 1; i < Stops.Length; i++)
        {
            if (position > Stops[i].Position) continue;
            var a = Stops[i - 1]; var b = Stops[i];
            double t = (position - a.Position) / (b.Position - a.Position);
            return Color.FromRgb((byte)(a.Color.R + (b.Color.R - a.Color.R) * t), (byte)(a.Color.G + (b.Color.G - a.Color.G) * t), (byte)(a.Color.B + (b.Color.B - a.Color.B) * t));
        }
        return Stops[^1].Color;
    }

    public static Geometry Sector(double minutes)
    {
        if (minutes >= 60) return new RectangleGeometry(new Rect(64, 62, 128, 128));
        if (minutes <= 0) return Geometry.Empty;
        // Radius slightly beyond the texture prevents an extra anti-aliased outer edge.
        const double radius = 66;
        double angle = minutes / 60 * Math.PI * 2;
        var geometry = new StreamGeometry();
        using (var dc = geometry.Open())
        {
            dc.BeginFigure(PixelArt.Center, true, true);
            dc.LineTo(new Point(128, 126 - radius), true, false);
            dc.ArcTo(new Point(128 + radius * Math.Sin(angle), 126 - radius * Math.Cos(angle)),
                new Size(radius, radius), 0, minutes > 30, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze(); return geometry;
    }
}
