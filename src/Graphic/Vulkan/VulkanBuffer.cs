using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanBuffer : IDisposable
{
    private Vk _vk;
    private Device _device;

    public Silk.NET.Vulkan.Buffer Handle;
    public DeviceMemory Memory;
    public void* Mapped;
    public ulong Size;

    public static VulkanBuffer CreateHostVisible(VulkanContext context, ulong size, BufferUsageFlags usage)
    {
        var buffer = new VulkanBuffer();
        buffer.Create(context, size, usage, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        return buffer;
    }

    public void Create(VulkanContext context, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        _vk = context.Vk;
        _device = context.Device;
        Size = size;

        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        Result bufferResult = _vk.CreateBuffer(_device, in createInfo, null, out Silk.NET.Vulkan.Buffer buffer);
        if (bufferResult != Result.Success)
        {
            throw new Exception($"vkCreateBuffer failed: {bufferResult}");
        }
        Handle = buffer;

        _vk.GetBufferMemoryRequirements(_device, Handle, out MemoryRequirements requirements);
        _vk.GetPhysicalDeviceMemoryProperties(context.PhysicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);

        uint memoryTypeIndex = FindMemoryType(memoryProperties, requirements.MemoryTypeBits, properties);

        var allocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        Result memoryResult = _vk.AllocateMemory(_device, in allocateInfo, null, out DeviceMemory memory);
        if (memoryResult != Result.Success)
        {
            throw new Exception($"vkAllocateMemory failed: {memoryResult}");
        }
        Memory = memory;

        Result bindResult = _vk.BindBufferMemory(_device, Handle, Memory, 0);
        if (bindResult != Result.Success)
        {
            throw new Exception($"vkBindBufferMemory failed: {bindResult}");
        }

        if (properties.HasFlag(MemoryPropertyFlags.HostVisibleBit))
        {
            void* mapped;
            Result mapResult = _vk.MapMemory(_device, Memory, 0, size, 0, &mapped);
            if (mapResult != Result.Success)
            {
                throw new Exception($"vkMapMemory failed: {mapResult}");
            }
            Mapped = mapped;
        }
    }

    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed (T* source = data)
        {
            System.Buffer.MemoryCopy(source, Mapped, Size, (ulong)(data.Length * sizeof(T)));
        }
    }

    private static uint FindMemoryType(PhysicalDeviceMemoryProperties memoryProperties, uint typeFilter, MemoryPropertyFlags properties)
    {
        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 && memoryProperties.MemoryTypes[(int)i].PropertyFlags.HasFlag(properties))
            {
                return i;
            }
        }
        throw new Exception("Не найден подходящий тип памяти GPU.");
    }

    public void Dispose()
    {
        if (Mapped != null)
        {
            _vk.UnmapMemory(_device, Memory);
            Mapped = null;
        }
        if (Handle.Handle != 0)
        {
            _vk.DestroyBuffer(_device, Handle, null);
            Handle = default;
        }
        if (Memory.Handle != 0)
        {
            _vk.FreeMemory(_device, Memory, null);
            Memory = default;
        }
    }
}
