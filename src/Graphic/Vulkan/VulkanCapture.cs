using System;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace Engine.Graphic.Vulkan;

public static unsafe class VulkanCapture
{
    public static void SaveSwapchainImage(VulkanContext context, VulkanSwapchain swapchain, uint imageIndex, string path)
    {
        uint width = swapchain.Extent.Width;
        uint height = swapchain.Extent.Height;
        ulong size = (ulong)width * height * 4;

        context.Vk.DeviceWaitIdle(context.Device);

        using VulkanBuffer buffer = VulkanBuffer.CreateHostVisible(context, size, BufferUsageFlags.TransferDstBit);

        VulkanCommands.ImmediateSubmit(context, commandBuffer =>
        {
            var barrier = new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                OldLayout = ImageLayout.PresentSrcKhr,
                NewLayout = ImageLayout.TransferSrcOptimal,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = swapchain.Images[imageIndex],
                SubresourceRange = new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    LevelCount = 1,
                    LayerCount = 1
                },
                SrcAccessMask = AccessFlags.MemoryReadBit,
                DstAccessMask = AccessFlags.TransferReadBit
            };
            context.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.BottomOfPipeBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &barrier);

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
                ImageExtent = new Extent3D { Width = width, Height = height, Depth = 1 }
            };
            context.Vk.CmdCopyImageToBuffer(commandBuffer, swapchain.Images[imageIndex], ImageLayout.TransferSrcOptimal, buffer.Handle, 1, &region);

            ImageMemoryBarrier back = barrier;
            back.OldLayout = ImageLayout.TransferSrcOptimal;
            back.NewLayout = ImageLayout.PresentSrcKhr;
            back.SrcAccessMask = AccessFlags.TransferReadBit;
            back.DstAccessMask = 0;
            context.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, &back);
        });

        byte[] pixels = new byte[size];
        System.Runtime.InteropServices.Marshal.Copy((IntPtr)buffer.Mapped, pixels, 0, (int)size);

        bool bgra = swapchain.Format == Format.B8G8R8A8Srgb || swapchain.Format == Format.B8G8R8A8Unorm;
        using ImageSharpImage image = bgra
            ? ImageSharpImage.LoadPixelData<Bgra32>(pixels, (int)width, (int)height)
            : ImageSharpImage.LoadPixelData<Rgba32>(pixels, (int)width, (int)height);
        image.SaveAsPng(path);
    }
}
