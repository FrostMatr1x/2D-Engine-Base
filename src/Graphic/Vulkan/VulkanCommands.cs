using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public static unsafe class VulkanCommands
{
    public static void ImmediateSubmit(VulkanContext context, Action<CommandBuffer> record)
    {
        var pool = new VulkanCommandPool();
        pool.Create(context, context.QueueFamilyIndices.GraphicsFamily!.Value, 1);

        CommandBuffer commandBuffer = pool.Buffers[0];
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Result beginResult = context.Vk.BeginCommandBuffer(commandBuffer, in beginInfo);
        if (beginResult != Result.Success)
        {
            throw new Exception($"vkBeginCommandBuffer failed: {beginResult}");
        }

        record(commandBuffer);

        Result endResult = context.Vk.EndCommandBuffer(commandBuffer);
        if (endResult != Result.Success)
        {
            throw new Exception($"vkEndCommandBuffer failed: {endResult}");
        }

        CommandBuffer submitBuffer = commandBuffer;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &submitBuffer
        };

        Result submitResult = context.Vk.QueueSubmit(context.GraphicsQueue, 1, in submitInfo, default);
        if (submitResult != Result.Success)
        {
            throw new Exception($"vkQueueSubmit failed: {submitResult}");
        }

        context.Vk.QueueWaitIdle(context.GraphicsQueue);
        pool.Dispose();
    }
}
