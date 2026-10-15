using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class FrameSync : IDisposable
{
    private Vk _vk;
    private Device _device;

    public Semaphore ImageAvailable;
    public Fence InFlight;

    public void Create(VulkanContext context, bool signaled)
    {
        _vk = context.Vk;
        _device = context.Device;

        var semaphoreInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        Result semaphoreResult = _vk.CreateSemaphore(_device, in semaphoreInfo, null, out ImageAvailable);
        if (semaphoreResult != Result.Success)
        {
            throw new Exception($"vkCreateSemaphore failed: {semaphoreResult}");
        }

        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = signaled ? FenceCreateFlags.SignaledBit : 0
        };
        Result fenceResult = _vk.CreateFence(_device, in fenceInfo, null, out InFlight);
        if (fenceResult != Result.Success)
        {
            throw new Exception($"vkCreateFence failed: {fenceResult}");
        }
    }

    public void Dispose()
    {
        if (ImageAvailable.Handle != 0)
        {
            _vk.DestroySemaphore(_device, ImageAvailable, null);
            ImageAvailable = default;
        }
        if (InFlight.Handle != 0)
        {
            _vk.DestroyFence(_device, InFlight, null);
            InFlight = default;
        }
    }
}
