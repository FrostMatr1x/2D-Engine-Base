using System.Runtime.InteropServices;

namespace Engine.Graphic;

[StructLayout(LayoutKind.Sequential)]
public struct Glyph
{
    public Rectangle Bounds;
    public int XOffset;
    public int YOffset;
    public float XAdvance;

    public Glyph(Rectangle bounds, int xOffset, int yOffset, float xAdvance)
    {
        Bounds = bounds;
        XOffset = xOffset;
        YOffset = yOffset;
        XAdvance = xAdvance;
    }

    public int Width => Bounds.Width;

    public int Height => Bounds.Height;

    public bool IsEmpty => Bounds.Width <= 0 || Bounds.Height <= 0;
}
