using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public static class VulkanMemory
{
    public static uint FindMemoryType(VulkanContext context, uint typeFilter, MemoryPropertyFlags properties)
    {
        context.Vk.GetPhysicalDeviceMemoryProperties(context.PhysicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);

        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 && memoryProperties.MemoryTypes[(int)i].PropertyFlags.HasFlag(properties))
            {
                return i;
            }
        }
        throw new Exception("Не найден подходящий тип памяти GPU.");
    }
}
