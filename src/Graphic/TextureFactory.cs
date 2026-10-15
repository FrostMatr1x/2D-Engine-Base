using System;
using Engine.Graphic.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public sealed class TextureFactory
{
    public TextureFactory(VulkanContext context, VulkanDescriptors descriptors)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
    }

    public VulkanContext Context { get; }

    public VulkanDescriptors Descriptors { get; }

    public Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(this, width, height);
    }

    public Texture2D CreateTexture(Image<Rgba32> image)
    {
        return new Texture2D(this, image);
    }
}
