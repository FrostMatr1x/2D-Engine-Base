using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanImage : IDisposable
{
    private Vk _vk;
    private Device _device;

    public Image Handle;
    public DeviceMemory Memory;
    public ImageView View;
    public int Width;
    public int Height;

    public void Create(VulkanContext context, int width, int height)
    {
        _vk = context.Vk;
        _device = context.Device;
        Width = width;
        Height = height;

        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = Format.R8G8B8A8Srgb,
            Extent = new Extent3D { Width = (uint)width, Height = (uint)height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };

        Result imageResult = _vk.CreateImage(_device, in createInfo, null, out Image image);
        if (imageResult != Result.Success)
        {
            throw new Exception($"vkCreateImage failed: {imageResult}");
        }
        Handle = image;

        _vk.GetImageMemoryRequirements(_device, Handle, out MemoryRequirements requirements);

        var allocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = FindMemoryType(context, requirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        Result memoryResult = _vk.AllocateMemory(_device, in allocateInfo, null, out DeviceMemory memory);
        if (memoryResult != Result.Success)
        {
            throw new Exception($"vkAllocateMemory (image) failed: {memoryResult}");
        }
        Memory = memory;

        Result bindResult = _vk.BindImageMemory(_device, Handle, Memory, 0);
        if (bindResult != Result.Success)
        {
            throw new Exception($"vkBindImageMemory failed: {bindResult}");
        }

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Handle,
            ViewType = ImageViewType.Type2D,
            Format = Format.R8G8B8A8Srgb,
            Components = new ComponentMapping
            {
                R = ComponentSwizzle.Identity,
                G = ComponentSwizzle.Identity,
                B = ComponentSwizzle.Identity,
                A = ComponentSwizzle.Identity
            },
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        Result viewResult = _vk.CreateImageView(_device, in viewInfo, null, out ImageView view);
        if (viewResult != Result.Success)
        {
            throw new Exception($"vkCreateImageView failed: {viewResult}");
        }
        View = view;
    }

    public void TransitionLayout(
        CommandBuffer commandBuffer,
        ImageLayout oldLayout,
        ImageLayout newLayout,
        PipelineStageFlags sourceStage,
        PipelineStageFlags destinationStage)
    {
        AccessFlags sourceAccess = oldLayout switch
        {
            ImageLayout.TransferDstOptimal => AccessFlags.TransferWriteBit,
            ImageLayout.TransferSrcOptimal => AccessFlags.TransferReadBit,
            ImageLayout.ShaderReadOnlyOptimal => AccessFlags.ShaderReadBit,
            _ => 0
        };

        AccessFlags destinationAccess = newLayout switch
        {
            ImageLayout.TransferDstOptimal => AccessFlags.TransferWriteBit,
            ImageLayout.TransferSrcOptimal => AccessFlags.TransferReadBit,
            ImageLayout.ShaderReadOnlyOptimal => AccessFlags.ShaderReadBit,
            _ => 0
        };

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = Handle,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = sourceAccess,
            DstAccessMask = destinationAccess
        };

        _vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, &barrier);
    }

    private static uint FindMemoryType(VulkanContext context, uint typeFilter, MemoryPropertyFlags properties)
    {
        context.Vk.GetPhysicalDeviceMemoryProperties(context.PhysicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);

        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 && memoryProperties.MemoryTypes[(int)i].PropertyFlags.HasFlag(properties))
            {
                return i;
            }
        }
        throw new Exception("Не найден подходящий тип памяти GPU для изображения.");
    }

    public void Dispose()
    {
        if (View.Handle != 0)
        {
            _vk.DestroyImageView(_device, View, null);
            View = default;
        }
        if (Handle.Handle != 0)
        {
            _vk.DestroyImage(_device, Handle, null);
            Handle = default;
        }
        if (Memory.Handle != 0)
        {
            _vk.FreeMemory(_device, Memory, null);
            Memory = default;
        }
    }
}
