using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VibeClock.Rendering;

/// <summary>Original code-drawn artwork. All coordinates use a 256 pixel artboard.</summary>
public static class PixelArt
{
    public const double Size = 256;
    public static readonly Point Center = new(128, 126);
    public const double KnobRadius = 32;
    private static readonly string[] Digits =
    ["111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
     "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111"];
    private static readonly string[] NumberColors =
    ["#FF619F", "#FF984E", "#F5C53E", "#85CE42", "#2AC597", "#35C4C8", "#4ABFE0", "#398DFA", "#A765E1", "#B269DF", "#F35CA8", "#F65EAA"];

    public static Color Color(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    public static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush(Color(hex)); brush.Freeze(); return brush;
    }
    private static LinearGradientBrush Gradient(string top, string bottom)
    {
        var brush = new LinearGradientBrush(Color(top), Color(bottom), 60); brush.Freeze(); return brush;
    }

    // Quantized rounded contours give the soft toy silhouette its small pixel steps.
    public static Geometry Round(double x, double y, double width, double height, double radius, int step = 2)
    {
        var points = new List<Point>();
        for (double row = 0; row <= height; row += step)
        {
            double d = row < radius ? radius - row : row > height - radius ? row - (height - radius) : 0;
            double inset = d > 0 ? Math.Round((radius - Math.Sqrt(Math.Max(0, radius * radius - d * d))) / step) * step : 0;
            points.Add(new Point(x + inset, y + row));
        }
        var geometry = new StreamGeometry();
        using (var c = geometry.Open())
        {
            c.BeginFigure(points[0], true, true);
            for (int i = 1; i < points.Count; i++)
            {
                c.LineTo(new Point(points[i - 1].X, points[i].Y), true, false);
                c.LineTo(points[i], true, false);
            }
            for (int i = points.Count - 1; i >= 0; i--)
            {
                double rx = x + width - (points[i].X - x);
                c.LineTo(new Point(rx, points[i].Y), true, false);
                if (i > 0) c.LineTo(new Point(rx, points[i - 1].Y), true, false);
            }
        }
        geometry.Freeze(); return geometry;
    }

    private static void Shape(DrawingContext dc, string color, double x, double y, double w, double h, double r) =>
        dc.DrawGeometry(Brush(color), null, Round(x, y, w, h, r));

    public static BitmapSource CreateBody() => Render(dc =>
    {
        Shape(dc, "#16A278B4", 17, 237, 222, 14, 10);
        Shape(dc, "#209D6CBA", 24, 237, 208, 10, 8);
        Shape(dc, "#7E579D", 7, 8, 242, 236, 44);
        Shape(dc, "#AA80C9", 9, 8, 238, 234, 44);
        dc.DrawGeometry(Gradient("#E9C7F2", "#AF83CD"), null, Round(11, 8, 234, 230, 42));
        dc.DrawGeometry(Gradient("#F1D6F8", "#C298DB"), null, Round(15, 10, 224, 222, 38));
        dc.DrawGeometry(Gradient("#DEB8EC", "#BA8ED4"), null, Round(21, 16, 218, 216, 36));
        Shape(dc, "#C59CDC", 27, 26, 204, 202, 32);
        Shape(dc, "#A87DC7", 30, 29, 198, 195, 29);
        Shape(dc, "#926AAF", 34, 33, 188, 188, 27);
        Shape(dc, "#735293", 36, 35, 184, 184, 26);
        Shape(dc, "#E5D8C8", 38, 37, 180, 180, 25);
        Shape(dc, "#EFE6D6", 40, 39, 176, 176, 23);
        dc.DrawGeometry(Gradient("#FFFCF2", "#FAF4E7"), null, Round(42, 41, 172, 172, 21));
        // Sparse stepped specular glints, matching the lavender plastic in the references.
        dc.DrawRectangle(Brush("#F4DEF9"), null, new Rect(52, 12, 129, 2));
        dc.DrawRectangle(Brush("#EED2F6"), null, new Rect(183, 14, 24, 2));
        dc.DrawRectangle(Brush("#FCEEFF"), null, new Rect(28, 26, 8, 4));
        dc.DrawRectangle(Brush("#FCEEFF"), null, new Rect(24, 30, 8, 6));
        dc.DrawRectangle(Brush("#F3DBFA"), null, new Rect(20, 36, 4, 8));
        dc.DrawRectangle(Brush("#EFD0F6"), null, new Rect(17, 50, 2, 121));
        dc.DrawRectangle(Brush("#D7AFE8"), null, new Rect(232, 57, 2, 135));
        dc.DrawRectangle(Brush("#E4C1F0"), null, new Rect(50, 227, 146, 2));
        for (int minute = 0; minute < 60; minute++)
        {
            double radians = minute * Math.PI / 30;
            bool major = minute % 5 == 0;
            double inner = major ? 64 : 67;
            double outer = 70;
            var pen = new Pen(Brush("#242333"), major ? 3 : 1.5);
            dc.DrawLine(pen,
                new Point(Math.Round(128 + Math.Sin(radians) * inner), Math.Round(126 - Math.Cos(radians) * inner)),
                new Point(Math.Round(128 + Math.Sin(radians) * outer), Math.Round(126 - Math.Cos(radians) * outer)));
        }
        for (int index = 0; index < 12; index++)
        {
            double angle = index * Math.PI / 6;
            DrawNumber(dc, (index * 5).ToString(CultureInfo.InvariantCulture),
                Math.Round(128 + Math.Sin(angle) * 79), Math.Round(126 - Math.Cos(angle) * 79), NumberColors[index]);
        }
    });

    private static void DrawNumber(DrawingContext dc, string value, double cx, double cy, string color)
    {
        const double pixel = 2;
        double left = cx - (value.Length * 4 - 1) * pixel / 2;
        var fill = Brush(color);
        for (int digit = 0; digit < value.Length; digit++)
            for (int bit = 0; bit < 15; bit++)
                if (Digits[value[digit] - '0'][bit] == '1')
                    dc.DrawRectangle(fill, null, new Rect(left + digit * 8 + bit % 3 * pixel, cy - 5 + bit / 3 * pixel, pixel, pixel));
    }

    // Symmetric two-pixel rows keep the knob circular rather than approximating it
    // with offset rounded rectangles of different aspect ratios.
    private static Geometry Circle(double cx, double cy, double radius)
    {
        var geometry = new StreamGeometry();
        using (var dc = geometry.Open())
        {
            for (double y = -radius; y < radius; y += 2)
            {
                double gridOffset = radius % 2;
                double extent = Math.Sqrt(radius * radius - (y + 1) * (y + 1));
                double halfWidth = 2 * Math.Floor((extent + 1 - gridOffset) / 2) + gridOffset;
                dc.BeginFigure(new Point(cx - halfWidth, cy + y), true, true);
                dc.LineTo(new Point(cx + halfWidth, cy + y), true, false);
                dc.LineTo(new Point(cx + halfWidth, cy + y + 2), true, false);
                dc.LineTo(new Point(cx - halfWidth, cy + y + 2), true, false);
            }
        }
        geometry.Freeze();
        return geometry;
    }

    public static BitmapSource CreateKnob() => Render(dc =>
    {
        double cx = Center.X, cy = Center.Y;
        dc.DrawGeometry(Brush("#16AA8D7B"), null, Circle(cx, cy + 4, 34));
        dc.DrawGeometry(Gradient("#AA7FC5", "#775193"), null, Circle(cx, cy, KnobRadius));
        dc.DrawGeometry(Gradient("#DFC0EE", "#A67CC1"), null, Circle(cx, cy, 30));
        // Restrict the grip grooves to the lower rim without changing its silhouette.
        dc.PushClip(Circle(cx, cy, 30));
        for (int i = -4; i <= 4; i++)
        {
            double x = cx + i * 6;
            dc.DrawRectangle(Brush("#956AB2"), null, new Rect(x - 1, cy + 12, 2, 20));
        }
        dc.Pop();
        dc.DrawGeometry(Brush("#AF83CA"), null, Circle(cx, cy, 28));
        dc.DrawGeometry(Gradient("#EACBF4", "#C49ADC"), null, Circle(cx, cy, 27));
        dc.DrawGeometry(Gradient("#DEB9ED", "#CAA1DE"), null, Circle(cx, cy, 25));
        dc.DrawRectangle(Brush("#F7E6FC"), null, new Rect(115, 105, 8, 3));
        dc.DrawRectangle(Brush("#F7E6FC"), null, new Rect(110, 108, 8, 4));
        dc.DrawRectangle(Brush("#EFD7F7"), null, new Rect(107, 114, 3, 4));
        dc.DrawRectangle(Brush("#E7C9F1"), null, new Rect(104, 122, 2, 7));
    });

    private static BitmapSource Render(Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);
        using (var dc = visual.RenderOpen()) draw(dc);
        var bitmap = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
}
