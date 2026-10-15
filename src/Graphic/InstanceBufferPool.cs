using System;
using Engine.Graphic.Vulkan;
using Silk.NET.Vulkan;

namespace Engine.Graphic;

public sealed unsafe class InstanceBufferPool : IDisposable
{
    private readonly VulkanBuffer[] _buffers;

    public InstanceBufferPool(VulkanContext context, int maxInstances, int framesInFlight)
    {
        _buffers = new VulkanBuffer[framesInFlight];
        ulong size = (ulong)(maxInstances * sizeof(Instance2D));

        for (int i = 0; i < framesInFlight; i++)
        {
            _buffers[i] = VulkanBuffer.CreateHostVisible(context, size, BufferUsageFlags.VertexBufferBit);
        }
    }

    public Silk.NET.Vulkan.Buffer GetBuffer(int frameIndex) => _buffers[frameIndex].Handle;

    public Instance2D* GetMapped(int frameIndex) => (Instance2D*)_buffers[frameIndex].Mapped;

    public void Dispose()
    {
        foreach (VulkanBuffer buffer in _buffers)
        {
            buffer.Dispose();
        }
    }
}
