using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Engine.Graphic.Vulkan;

namespace Engine.Graphic;

public sealed class GraphicsDevice
{
    private readonly Renderer2D _renderer;

    public GraphicsDevice(VulkanContext context, Renderer2D renderer)
    {
        Context = context;
        _renderer = renderer;
        Textures = new TextureFactory(context, renderer.Descriptors);
    }

    public VulkanContext Context { get; }

    public TextureFactory Textures { get; }

    public int FramesInFlight => _renderer.FramesInFlight;

    public Viewport Viewport => new Viewport(0, 0, _renderer.BackbufferWidth, _renderer.BackbufferHeight);

    public Color ClearColor { get; set; } = new Color(13, 15, 23, 255);

    public Texture2D CreateTexture(int width, int height) => Textures.CreateTexture(width, height);

    public Texture2D CreateTexture(Image<Rgba32> image) => Textures.CreateTexture(image);

    public void Clear(Color color) => _renderer.ClearColor = color;
}
