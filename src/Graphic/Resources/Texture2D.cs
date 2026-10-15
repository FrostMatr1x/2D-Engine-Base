using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Graphic.Vulkan;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public sealed class Texture2D : IDisposable
{
    private readonly VulkanContext _context;
    private readonly VulkanDescriptors _descriptors;

    public int Width;
    public int Height;

    private VulkanImage _image;

    public ImageView View => _image.View;
    public DescriptorSet DescriptorSet { get; private set; }

    public bool IsValid => _context != null && _image != null;

    internal Texture2D(TextureFactory factory, int width, int height)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        }

        _context = factory.Context;
        _descriptors = factory.Descriptors;
        Width = width;
        Height = height;
        Create(new byte[width * height * 4]);
    }

    internal Texture2D(TextureFactory factory, Image<Rgba32> image)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        _context = factory.Context;
        _descriptors = factory.Descriptors;
        Width = image.Width;
        Height = image.Height;
        Create(ToBytes(image));
    }

    public Texture2D(GraphicsDevice device, int width, int height) : this(device.Textures, width, height)
    {
    }

    public Texture2D(GraphicsDevice device, Image<Rgba32> image) : this(device.Textures, image)
    {
    }

    private static byte[] ToBytes(Image<Rgba32> image)
    {
        byte[] pixels = new byte[image.Width * image.Height * 4];
        if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory))
        {
            System.Runtime.InteropServices.MemoryMarshal.AsBytes(pixelMemory.Span).CopyTo(pixels);
        }
        else
        {
            image.CopyPixelDataTo(pixels);
        }
        return pixels;
    }

    public void SetData(Color[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length != Width * Height)
        {
            throw new ArgumentException(
                $"Data length {data.Length} does not match texture size {Width}x{Height}.",
                nameof(data));
        }

        byte[] pixels = new byte[data.Length * 4];
        for (int i = 0; i < data.Length; i++)
        {
            Color color = data[i];
            int offset = i * 4;
            pixels[offset] = color.R;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.B;
            pixels[offset + 3] = color.A;
        }

        Upload(pixels);
    }

    public void SetData<T>(T[] data) where T : unmanaged
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        int byteCount = Width * Height * 4;
        if (data.Length * Unsafe.SizeOf<T>() != byteCount)
        {
            throw new ArgumentException(
                $"Data length {data.Length} * {Unsafe.SizeOf<T>()} does not match texture byte count {byteCount}.",
                nameof(data));
        }

        Upload(MemoryMarshal.AsBytes(data.AsSpan()).ToArray());
    }

    public void GetData<T>(T[] data) where T : unmanaged
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        int byteCount = Width * Height * 4;
        if (data.Length * Unsafe.SizeOf<T>() != byteCount)
        {
            throw new ArgumentException(
                $"Data length {data.Length} * {Unsafe.SizeOf<T>()} does not match texture byte count {byteCount}.",
                nameof(data));
        }

        Readback(data);
    }

    private unsafe void Readback<T>(T[] data) where T : unmanaged
    {
        ulong bufferSize = (ulong)(Width * Height * 4);
        using VulkanBuffer staging = VulkanBuffer.CreateHostVisible(
            _context,
            bufferSize,
            BufferUsageFlags.TransferDstBit);

        VulkanCommands.ImmediateSubmit(_context, commandBuffer =>
        {
            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.ShaderReadOnlyOptimal,
                ImageLayout.TransferSrcOptimal,
                PipelineStageFlags.FragmentShaderBit,
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

            _context.Vk.CmdCopyImageToBuffer(
                commandBuffer,
                _image.Handle,
                ImageLayout.TransferSrcOptimal,
                staging.Handle,
                1,
                &region);

            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.TransferSrcOptimal,
                ImageLayout.ShaderReadOnlyOptimal,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.FragmentShaderBit);
        });

        fixed (T* destination = data)
        {
            System.Buffer.MemoryCopy(staging.Mapped, destination, bufferSize, bufferSize);
        }
    }

    private unsafe void Create(byte[] pixels)
    {
        _image = new VulkanImage();
        _image.Create(_context, Width, Height);

        using VulkanBuffer staging = CreateStaging(pixels);

        VulkanCommands.ImmediateSubmit(_context, commandBuffer =>
        {
            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.Undefined,
                ImageLayout.TransferDstOptimal,
                PipelineStageFlags.TopOfPipeBit,
                PipelineStageFlags.TransferBit);

            RecordCopy(commandBuffer, staging.Handle);

            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.TransferDstOptimal,
                ImageLayout.ShaderReadOnlyOptimal,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.FragmentShaderBit);
        });

        DescriptorSet = _descriptors.AllocateTextureSet(_image.View);
    }

    private unsafe void Upload(byte[] pixels)
    {
        using VulkanBuffer staging = CreateStaging(pixels);

        VulkanCommands.ImmediateSubmit(_context, commandBuffer =>
        {
            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.ShaderReadOnlyOptimal,
                ImageLayout.TransferDstOptimal,
                PipelineStageFlags.FragmentShaderBit,
                PipelineStageFlags.TransferBit);

            RecordCopy(commandBuffer, staging.Handle);

            _image.TransitionLayout(
                commandBuffer,
                ImageLayout.TransferDstOptimal,
                ImageLayout.ShaderReadOnlyOptimal,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.FragmentShaderBit);
        });
    }

    private VulkanBuffer CreateStaging(byte[] pixels)
    {
        ulong bufferSize = (ulong)(Width * Height * 4);
        VulkanBuffer staging = VulkanBuffer.CreateHostVisible(_context, bufferSize, BufferUsageFlags.TransferSrcBit);
        staging.SetData(pixels);
        return staging;
    }

    private unsafe void RecordCopy(CommandBuffer commandBuffer, Silk.NET.Vulkan.Buffer staging)
    {
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
            staging,
            _image.Handle,
            ImageLayout.TransferDstOptimal,
            1,
            &region);
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
