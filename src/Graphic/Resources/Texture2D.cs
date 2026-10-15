using System;
using Engine.Graphic.Vulkan;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public sealed class Texture2D : IDisposable
{
    private static VulkanContext _context;
    private static VulkanDescriptors _descriptors;

    public int Width;
    public int Height;

    private VulkanImage _image;

    public ImageView View => _image.View;
    public DescriptorSet DescriptorSet { get; private set; }

    public bool IsValid => _context != null && _image != null;

    public static void Init(VulkanContext context, VulkanDescriptors descriptors)
    {
        _context = context;
        _descriptors = descriptors;
    }

    public Texture2D(Image<Rgba32> image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }
        if (_context == null)
        {
            throw new InvalidOperationException("Texture2D.Init must be called before creating textures.");
        }

        Width = image.Width;
        Height = image.Height;

        byte[] pixels = new byte[Width * Height * 4];
        if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory))
        {
            System.Runtime.InteropServices.MemoryMarshal.AsBytes(pixelMemory.Span).CopyTo(pixels);
        }
        else
        {
            image.CopyPixelDataTo(pixels);
        }

        Create(pixels);
    }

    public Texture2D() : this(CreateWhitePixelImage())
    {
    }

    private unsafe void Create(byte[] pixels)
    {
        _image = new VulkanImage();
        _image.Create(_context, Width, Height);

        ulong bufferSize = (ulong)(Width * Height * 4);
        using VulkanBuffer staging = VulkanBuffer.CreateHostVisible(_context, bufferSize, BufferUsageFlags.TransferSrcBit);
        staging.SetData(pixels);

        VulkanCommands.ImmediateSubmit(_context, commandBuffer =>
        {
            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.Undefined,
                ImageLayout.TransferDstOptimal,
                PipelineStageFlags.TopOfPipeBit,
                PipelineStageFlags.TransferBit);

            var region = new BufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                ImageOffset = new Offset3D { X = 0, Y = 0, Z = 0 },
                ImageExtent = new Extent3D { Width = (uint)Width, Height = (uint)Height, Depth = 1 }
            };

            _context.Vk.CmdCopyBufferToImage(
                commandBuffer,
                staging.Handle,
                _image.Handle,
                ImageLayout.TransferDstOptimal,
                1,
                &region);

            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.TransferDstOptimal,
                ImageLayout.ShaderReadOnlyOptimal,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.FragmentShaderBit);
        });

        DescriptorSet = _descriptors.AllocateTextureSet(_image.View);
    }

    private static Image<Rgba32> CreateWhitePixelImage()
    {
        var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(255, 255, 255, 255);
        return image;
    }

    public void Dispose()
    {
        if (_image == null)
        {
            return;
        }

        if (DescriptorSet.Handle != 0)
        {
            _descriptors.FreeTextureSet(DescriptorSet);
            DescriptorSet = default;
        }

        _image.Dispose();
        _image = null;
    }
}
