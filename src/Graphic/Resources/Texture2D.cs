using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public class Texture2D
{
    public static Vk Vk;
    public static void Init(Vk vk)
    {
        Vk = vk;
    }

    public Texture2D(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;

        byte[] rawPixels = new byte[width * height * 4];

        if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory))
        {
            MemoryMarshal.AsBytes(pixelMemory.Span).CopyTo(rawPixels);
        }
        else
        {
            image.CopyPixelDataTo(rawPixels);
        }

        //UploadToGPU(rawPixels);
    }

    #warning Enable the use of a custom 2D texture without a source texture.
    public Texture2D()
    {
        
    }

    private void UploadToGPU(byte[] bytes, int size)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (uint)size,
            Usage = BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive 
        };

        Silk.NET.Vulkan.Buffer stagingBuffer;
    }
}