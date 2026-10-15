using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanRenderPass : IDisposable
{
    private Vk _vk;
    private Device _device;

    public RenderPass Handle;
    public Framebuffer[] Framebuffers = Array.Empty<Framebuffer>();
    public ClearValue ClearColor = new ClearValue(new ClearColorValue(0.05f, 0.06f, 0.09f, 1.0f));

    public void Create(VulkanContext context, Format colorFormat)
    {
        _vk = context.Vk;
        _device = context.Device;

        var colorAttachment = new AttachmentDescription
        {
            Format = colorFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var colorReference = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorReference
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };

        var createInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };

        Result result = _vk.CreateRenderPass(_device, in createInfo, null, out RenderPass renderPass);
        if (result != Result.Success)
        {
            throw new Exception($"vkCreateRenderPass failed: {result}");
        }
        Handle = renderPass;
    }

    public void CreateFramebuffers(VulkanContext context, VulkanSwapchain swapchain)
    {
        Framebuffers = new Framebuffer[swapchain.ImageViews.Length];
        for (int i = 0; i < swapchain.ImageViews.Length; i++)
        {
            ImageView attachment = swapchain.ImageViews[i];
            var createInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = Handle,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = swapchain.Extent.Width,
                Height = swapchain.Extent.Height,
                Layers = 1
            };

            Result result = _vk.CreateFramebuffer(_device, in createInfo, null, out Framebuffers[i]);
            if (result != Result.Success)
            {
                throw new Exception($"vkCreateFramebuffer failed: {result}");
            }
        }
    }

    public void DestroyFramebuffers()
    {
        foreach (Framebuffer framebuffer in Framebuffers)
        {
            if (framebuffer.Handle != 0)
            {
                _vk.DestroyFramebuffer(_device, framebuffer, null);
            }
        }
        Framebuffers = Array.Empty<Framebuffer>();
    }

    public void Dispose()
    {
        DestroyFramebuffers();
        if (Handle.Handle != 0)
        {
            _vk.DestroyRenderPass(_device, Handle, null);
            Handle = default;
        }
    }
}
