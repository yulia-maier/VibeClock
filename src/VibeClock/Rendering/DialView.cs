using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VibeClock.Rendering;

public sealed class DialView : FrameworkElement
{
    private readonly BitmapSource body = PixelArt.CreateBody();
    private readonly BitmapSource rainbow = RainbowProgress.CreateTexture();
    private readonly BitmapSource knob = PixelArt.CreateKnob();
    private static readonly Brush PointerEdge = PixelArt.Brush("#805C9F");
    private static readonly Brush PointerFill = PixelArt.Brush("#CCA3E3");
    private static readonly Brush PointerGlint = PixelArt.Brush("#EBD3F7");
    // Shorten the exposed part by 30%; keep its width and attachment beneath the knob.
    private static readonly double PointerTipY = PixelArt.Center.Y -
        (PixelArt.KnobRadius + (58 - PixelArt.KnobRadius) * 0.7);
    private double minutes;
    public double Minutes
    {
        get => minutes;
        set { minutes = Math.Clamp(value, 0, 60); InvalidateVisual(); }
    }

    public DialView()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        Focusable = true;
        System.Windows.Automation.AutomationProperties.SetName(this, "VibeClock. Круговой таймер на 60 минут");
        System.Windows.Automation.AutomationProperties.SetHelpText(this, "Стрелки: выбрать минуты. Enter: запустить. Пробел: пауза. Escape: сброс. Контекстное меню: Shift+F10.");
    }

    public Point ToArtboard(Point point) => new(point.X / ActualWidth * 256, point.Y / ActualHeight * 256);

    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth / 256, ActualHeight / 256));
        dc.DrawImage(body, new Rect(0, 0, 256, 256));
        if (Minutes > 0)
        {
            dc.PushClip(RainbowProgress.Sector(Minutes));
            dc.DrawImage(rainbow, new Rect(64, 62, 128, 128));
            dc.Pop();
        }
        dc.PushTransform(new RotateTransform(Minutes * 6, 128, 126));
        dc.DrawGeometry(PointerEdge, null, PixelArt.Round(123, PointerTipY, 10, 108 - PointerTipY, 4));
        dc.DrawGeometry(PointerFill, null, PixelArt.Round(125, PointerTipY + 2, 6, 104 - PointerTipY, 2));
        dc.DrawRectangle(PointerGlint, null, new Rect(125, PointerTipY + 6, 2, 94 - PointerTipY));
        dc.Pop();
        dc.DrawImage(knob, new Rect(0, 0, 256, 256));
        dc.Pop();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
