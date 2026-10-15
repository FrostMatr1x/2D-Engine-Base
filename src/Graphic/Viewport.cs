namespace Engine.Graphic;

public struct Viewport
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public Viewport(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int MinDepth => 0;

    public int MaxDepth => 1;

    public Rectangle Bounds => new Rectangle(X, Y, Width, Height);
}
