using System;
using System.IO;
using Engine.Graphic.Vulkan;
using Silk.NET.Vulkan;

namespace Engine.Graphic;

public sealed unsafe class Renderer2D : IDisposable
{
    private readonly VulkanContext _context;
    private readonly EngineWindow _window;
    private readonly int _framesInFlight;
    private readonly int _maxInstances;

    private readonly VulkanSwapchain _swapchain;
    private readonly VulkanRenderPass _renderPass;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanCommandPool _commandPool;
    private readonly FrameSync[] _frames;
    private readonly VulkanBuffer _vertexBuffer;
    private readonly VulkanBuffer _indexBuffer;
    private readonly InstanceBufferPool _instanceBuffers;

    private Semaphore[] _renderFinished = Array.Empty<Semaphore>();
    private int _currentFrame;
    private bool _framebufferResized;
    private bool _disposed;

    public Renderer2D(VulkanContext context, EngineWindow window, int maxInstances, int framesInFlight = 2)
    {
        _context = context;
        _window = window;
        _maxInstances = maxInstances;
        _framesInFlight = framesInFlight;

        _swapchain = new VulkanSwapchain();
        _swapchain.Create(context, window);

        _renderPass = new VulkanRenderPass();
        _renderPass.Create(context, _swapchain.Format);
        _renderPass.CreateFramebuffers(context, _swapchain);

        _pipeline = new VulkanPipeline();
        _pipeline.Create(
            context,
            _renderPass.Handle,
            ShaderPath("quad.vert.spv"),
            ShaderPath("quad.frag.spv"),
            CreateVertexBindings(),
            CreateVertexAttributes(),
            (uint)sizeof(PushConstants));

        _commandPool = new VulkanCommandPool();
        _commandPool.Create(context, context.QueueFamilyIndices.GraphicsFamily!.Value, framesInFlight);

        _frames = new FrameSync[framesInFlight];
        for (int i = 0; i < framesInFlight; i++)
        {
            _frames[i] = new FrameSync();
            _frames[i].Create(context, true);
        }

        CreateRenderFinished();

        _vertexBuffer = VulkanBuffer.CreateHostVisible(context, (ulong)(4 * sizeof(Vertex2D)), BufferUsageFlags.VertexBufferBit);
        _vertexBuffer.SetData(QuadVertices());

        _indexBuffer = VulkanBuffer.CreateHostVisible(context, (ulong)(6 * sizeof(ushort)), BufferUsageFlags.IndexBufferBit);
        _indexBuffer.SetData(QuadIndices());

        _instanceBuffers = new InstanceBufferPool(context, maxInstances, framesInFlight);
    }

    public void NotifyFramebufferResized()
    {
        _framebufferResized = true;
    }

    public void DrawFrame(Camera2D camera, ReadOnlySpan<Instance2D> instances)
    {
        if (_framebufferResized && !RecreateSwapchain())
        {
            return;
        }

        FrameSync sync = _frames[_currentFrame];
        Fence inFlight = sync.InFlight;
        _context.Vk.WaitForFences(_context.Device, 1, &inFlight, true, ulong.MaxValue);

        Semaphore imageAvailable = sync.ImageAvailable;
        uint imageIndex = 0;
        Result acquire = _context.KhrSwapchain.AcquireNextImage(_context.Device, _swapchain.Handle, ulong.MaxValue, imageAvailable, default, ref imageIndex);
        if (acquire == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return;
        }
        if (acquire != Result.Success && acquire != Result.SuboptimalKhr)
        {
            throw new Exception($"vkAcquireNextImageKHR failed: {acquire}");
        }

        _context.Vk.ResetFences(_context.Device, 1, &inFlight);

        int count = Math.Min(instances.Length, _maxInstances);
        Instance2D* mapped = _instanceBuffers.GetMapped(_currentFrame);
        for (int i = 0; i < count; i++)
        {
            mapped[i] = instances[i];
        }

        CommandBuffer commandBuffer = _commandPool.Buffers[_currentFrame];
        _context.Vk.ResetCommandBuffer(commandBuffer, 0);
        Record(commandBuffer, imageIndex, (uint)count, camera);

        PipelineStageFlags waitStage = PipelineStageFlags.ColorAttachmentOutputBit;
        Semaphore signal = _renderFinished[imageIndex];
        CommandBuffer submitCommandBuffer = commandBuffer;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &imageAvailable,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &submitCommandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signal
        };

        Result submit = _context.Vk.QueueSubmit(_context.GraphicsQueue, 1, in submitInfo, inFlight);
        if (submit != Result.Success)
        {
            throw new Exception($"vkQueueSubmit failed: {submit}");
        }

        SwapchainKHR swapchain = _swapchain.Handle;
        uint presentIndex = imageIndex;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signal,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &presentIndex
        };

        Result present = _context.KhrSwapchain.QueuePresent(_context.PresentQueue, in presentInfo);
        if (present == Result.ErrorOutOfDateKhr || present == Result.SuboptimalKhr || _framebufferResized)
        {
            RecreateSwapchain();
        }
        else if (present != Result.Success)
        {
            throw new Exception($"vkQueuePresentKHR failed: {present}");
        }

        _currentFrame = (_currentFrame + 1) % _framesInFlight;
    }

    private void Record(CommandBuffer commandBuffer, uint imageIndex, uint instanceCount, Camera2D camera)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo
        };
        _context.Vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        ClearValue clearValue = _renderPass.ClearColor;
        var renderPassBegin = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass.Handle,
            Framebuffer = _renderPass.Framebuffers[imageIndex],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D { X = 0, Y = 0 },
                Extent = _swapchain.Extent
            },
            ClearValueCount = 1,
            PClearValues = &clearValue
        };
        _context.Vk.CmdBeginRenderPass(commandBuffer, in renderPassBegin, SubpassContents.Inline);

        var viewport = new Viewport
        {
            X = 0f,
            Y = 0f,
            Width = _swapchain.Extent.Width,
            Height = _swapchain.Extent.Height,
            MinDepth = 0f,
            MaxDepth = 1f
        };
        _context.Vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);

        var scissor = new Rect2D
        {
            Offset = new Offset2D { X = 0, Y = 0 },
            Extent = _swapchain.Extent
        };
        _context.Vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        _context.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, _pipeline.Handle);

        if (instanceCount > 0)
        {
            Silk.NET.Vulkan.Buffer* vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[2];
            vertexBuffers[0] = _vertexBuffer.Handle;
            vertexBuffers[1] = _instanceBuffers.GetBuffer(_currentFrame);

            ulong* offsets = stackalloc ulong[2];
            offsets[0] = 0;
            offsets[1] = 0;
            _context.Vk.CmdBindVertexBuffers(commandBuffer, 0, 2, vertexBuffers, offsets);

            _context.Vk.CmdBindIndexBuffer(commandBuffer, _indexBuffer.Handle, 0, IndexType.Uint16);

            var pushConstants = new PushConstants
            {
                Projection = camera.GetViewProjection(_swapchain.Extent.Width, _swapchain.Extent.Height)
            };
            _context.Vk.CmdPushConstants(commandBuffer, _pipeline.Layout, ShaderStageFlags.VertexBit, 0, (uint)sizeof(PushConstants), &pushConstants);

            _context.Vk.CmdDrawIndexed(commandBuffer, 6, instanceCount, 0, 0, 0);
        }

        _context.Vk.CmdEndRenderPass(commandBuffer);
        _context.Vk.EndCommandBuffer(commandBuffer);
    }

    private bool RecreateSwapchain()
    {
        if (!_swapchain.WaitForNonZeroExtent(_window))
        {
            return false;
        }

        _context.Vk.DeviceWaitIdle(_context.Device);

        _renderPass.DestroyFramebuffers();
        DestroyRenderFinished();
        _swapchain.Recreate(_context, _window);
        _renderPass.CreateFramebuffers(_context, _swapchain);
        CreateRenderFinished();

        _framebufferResized = false;
        return true;
    }

    private void CreateRenderFinished()
    {
        _renderFinished = new Semaphore[_swapchain.ImageCount];
        var createInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        for (int i = 0; i < _renderFinished.Length; i++)
        {
            _context.Vk.CreateSemaphore(_context.Device, in createInfo, null, out _renderFinished[i]);
        }
    }

    private void DestroyRenderFinished()
    {
        foreach (Semaphore semaphore in _renderFinished)
        {
            if (semaphore.Handle != 0)
            {
                _context.Vk.DestroySemaphore(_context.Device, semaphore, null);
            }
        }
        _renderFinished = Array.Empty<Semaphore>();
    }

    private static Vertex2D[] QuadVertices()
    {
        return new[]
        {
            new Vertex2D(0f, 0f),
            new Vertex2D(1f, 0f),
            new Vertex2D(1f, 1f),
            new Vertex2D(0f, 1f)
        };
    }

    private static ushort[] QuadIndices()
    {
        return new ushort[] { 0, 1, 2, 0, 2, 3 };
    }

    private static VertexInputBindingDescription[] CreateVertexBindings()
    {
        return new[]
        {
            new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)sizeof(Vertex2D),
                InputRate = VertexInputRate.Vertex
            },
            new VertexInputBindingDescription
            {
                Binding = 1,
                Stride = (uint)sizeof(Instance2D),
                InputRate = VertexInputRate.Instance
            }
        };
    }

    private static VertexInputAttributeDescription[] CreateVertexAttributes()
    {
        return new[]
        {
            new VertexInputAttributeDescription { Location = 0, Binding = 0, Format = Format.R32G32Sfloat, Offset = 0 },
            new VertexInputAttributeDescription { Location = 1, Binding = 1, Format = Format.R32G32Sfloat, Offset = 0 },
            new VertexInputAttributeDescription { Location = 2, Binding = 1, Format = Format.R32G32Sfloat, Offset = 8 },
            new VertexInputAttributeDescription { Location = 3, Binding = 1, Format = Format.R32Sfloat, Offset = 16 },
            new VertexInputAttributeDescription { Location = 4, Binding = 1, Format = Format.R32Uint, Offset = 20 }
        };
    }

    private static string ShaderPath(string fileName)
    {
        string relative = Path.Combine("Content", "Shaders", fileName);
        if (File.Exists(relative))
        {
            return relative;
        }
        return Path.Combine(AppContext.BaseDirectory, relative);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _context.Vk.DeviceWaitIdle(_context.Device);

        DestroyRenderFinished();
        foreach (FrameSync frame in _frames)
        {
            frame.Dispose();
        }
        _commandPool.Dispose();
        _renderPass.Dispose();
        _pipeline.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _instanceBuffers.Dispose();
        _swapchain.Dispose();
    }
}
