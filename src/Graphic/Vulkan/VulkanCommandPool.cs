using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanCommandPool : IDisposable
{
    private Vk _vk;
    private Device _device;

    public CommandPool Handle;
    public CommandBuffer[] Buffers = Array.Empty<CommandBuffer>();

    public void Create(VulkanContext context, uint queueFamilyIndex, int bufferCount)
    {
        _vk = context.Vk;
        _device = context.Device;

        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndex
        };

        Result result = _vk.CreateCommandPool(_device, in createInfo, null, out Handle);
        if (result != Result.Success)
        {
            throw new Exception($"vkCreateCommandPool failed: {result}");
        }

        var allocateInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = Handle,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)bufferCount
        };

        Buffers = new CommandBuffer[bufferCount];
        fixed (CommandBuffer* pBuffers = Buffers)
        {
            Result allocateResult = _vk.AllocateCommandBuffers(_device, in allocateInfo, pBuffers);
            if (allocateResult != Result.Success)
            {
                throw new Exception($"vkAllocateCommandBuffers failed: {allocateResult}");
            }
        }
    }

    public void Dispose()
    {
        if (Handle.Handle != 0)
        {
            _vk.DestroyCommandPool(_device, Handle, null);
            Handle = default;
        }
    }
}
