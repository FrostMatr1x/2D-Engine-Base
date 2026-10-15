using System;
using System.Threading;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanSwapchain : IDisposable
{
    private Vk _vk;
    private Device _device;
    private KhrSwapchain _khrSwapchain;
    private KhrSurface _khrSurface;
    private PhysicalDevice _physicalDevice;
    private SurfaceKHR _surface;
    private QueueFamilyIndices _queueFamilyIndices;

    public SwapchainKHR Handle;
    public Image[] Images = Array.Empty<Image>();
    public ImageView[] ImageViews = Array.Empty<ImageView>();
    public Format Format;
    public ColorSpaceKHR ColorSpace;
    public Extent2D Extent;
    public uint ImageCount;

    public void Create(VulkanContext context, EngineWindow window)
    {
        _vk = context.Vk;
        _device = context.Device;
        _khrSwapchain = context.KhrSwapchain;
        _khrSurface = context.KhrSurface;
        _physicalDevice = context.PhysicalDevice;
        _surface = context.Surface;
        _queueFamilyIndices = context.QueueFamilyIndices;

        CreateInternal(window, default);
    }

    public void Recreate(VulkanContext context, EngineWindow window)
    {
        SwapchainKHR old = Handle;
        DestroyImageViews();
        Handle = default;

        CreateInternal(window, old);

        if (old.Handle != 0)
        {
            _khrSwapchain.DestroySwapchain(_device, old, null);
        }
    }

    private void CreateInternal(EngineWindow window, SwapchainKHR old)
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out SurfaceCapabilitiesKHR capabilities);

        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];
        fixed (SurfaceFormatKHR* pFormats = formats)
        {
            _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref formatCount, pFormats);
        }

        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref presentModeCount, null);
        var presentModes = new PresentModeKHR[presentModeCount];
        fixed (PresentModeKHR* pPresentModes = presentModes)
        {
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref presentModeCount, pPresentModes);
        }

        SurfaceFormatKHR surfaceFormat = ChooseFormat(formats, out ColorSpaceKHR colorSpace);
        PresentModeKHR presentMode = ChoosePresentMode(presentModes);
        Extent2D extent = ChooseExtent(capabilities, window);

        uint imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
        {
            imageCount = capabilities.MaxImageCount;
        }

        uint graphicsFamily = _queueFamilyIndices.GraphicsFamily!.Value;
        uint presentFamily = _queueFamilyIndices.PresentFamily!.Value;
        bool singleFamily = graphicsFamily == presentFamily;
        uint[] familyIndices = { graphicsFamily, presentFamily };

        Result result;
        SwapchainKHR swapchain;
        fixed (uint* pFamilyIndices = familyIndices)
        {
            var createInfo = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = _surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = colorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = singleFamily ? SharingMode.Exclusive : SharingMode.Concurrent,
                QueueFamilyIndexCount = singleFamily ? 0u : 2u,
                PQueueFamilyIndices = singleFamily ? null : pFamilyIndices,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                OldSwapchain = old
            };

            result = _khrSwapchain.CreateSwapchain(_device, in createInfo, null, out swapchain);
        }

        if (result != Result.Success)
        {
            throw new Exception($"vkCreateSwapchainKHR failed: {result}");
        }
        Handle = swapchain;
        Format = surfaceFormat.Format;
        ColorSpace = colorSpace;
        Extent = extent;

        uint actualCount = 0;
        _khrSwapchain.GetSwapchainImages(_device, Handle, ref actualCount, null);
        Images = new Image[actualCount];
        fixed (Image* pImages = Images)
        {
            _khrSwapchain.GetSwapchainImages(_device, Handle, ref actualCount, pImages);
        }
        ImageCount = actualCount;

        CreateImageViews();
    }

    private void CreateImageViews()
    {
        ImageViews = new ImageView[Images.Length];
        for (int i = 0; i < Images.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = Images[i],
                ViewType = ImageViewType.Type2D,
                Format = Format,
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

            Result result = _vk.CreateImageView(_device, in createInfo, null, out ImageViews[i]);
            if (result != Result.Success)
            {
                throw new Exception($"vkCreateImageView failed: {result}");
            }
        }
    }

    private static SurfaceFormatKHR ChooseFormat(SurfaceFormatKHR[] formats, out ColorSpaceKHR colorSpace)
    {
        if (formats.Length == 0)
        {
            colorSpace = ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr;
            return new SurfaceFormatKHR { Format = Format.B8G8R8A8Srgb, ColorSpace = colorSpace };
        }

        if (formats.Length == 1 && formats[0].Format == Format.Undefined)
        {
            colorSpace = formats[0].ColorSpace;
            return new SurfaceFormatKHR { Format = Format.B8G8R8A8Srgb, ColorSpace = formats[0].ColorSpace };
        }

        colorSpace = ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr;

        foreach (SurfaceFormatKHR format in formats)
        {
            if (format.Format == Format.B8G8R8A8Srgb && format.ColorSpace == ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr)
            {
                colorSpace = format.ColorSpace;
                return format;
            }
        }

        foreach (SurfaceFormatKHR format in formats)
        {
            if (format.Format == Format.R8G8B8A8Srgb && format.ColorSpace == ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr)
            {
                colorSpace = format.ColorSpace;
                return format;
            }
        }

        colorSpace = formats[0].ColorSpace;
        return formats[0];
    }

    private static PresentModeKHR ChoosePresentMode(PresentModeKHR[] presentModes)
    {
        foreach (PresentModeKHR mode in presentModes)
        {
            if (mode == PresentModeKHR.FifoKhr)
            {
                return PresentModeKHR.FifoKhr;
            }
        }
        return PresentModeKHR.FifoKhr;
    }

    private static Extent2D ChooseExtent(SurfaceCapabilitiesKHR capabilities, EngineWindow window)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }

        uint width = (uint)Math.Clamp(window.FramebufferWidth, (int)capabilities.MinImageExtent.Width, (int)capabilities.MaxImageExtent.Width);
        uint height = (uint)Math.Clamp(window.FramebufferHeight, (int)capabilities.MinImageExtent.Height, (int)capabilities.MaxImageExtent.Height);
        return new Extent2D { Width = width, Height = height };
    }

    public bool WaitForNonZeroExtent(EngineWindow window)
    {
        while ((window.FramebufferWidth == 0 || window.FramebufferHeight == 0) && !window.IsClosing)
        {
            window.DoEvents();
            Thread.Sleep(10);
        }
        return window.FramebufferWidth > 0 && window.FramebufferHeight > 0;
    }

    private void DestroyImageViews()
    {
        foreach (ImageView view in ImageViews)
        {
            if (view.Handle != 0)
            {
                _vk.DestroyImageView(_device, view, null);
            }
        }
        ImageViews = Array.Empty<ImageView>();
        Images = Array.Empty<Image>();
        ImageCount = 0;
    }

    public void Destroy()
    {
        DestroyImageViews();

        if (Handle.Handle != 0)
        {
            _khrSwapchain.DestroySwapchain(_device, Handle, null);
            Handle = default;
        }
    }

    public void Dispose()
    {
        Destroy();
    }
}
