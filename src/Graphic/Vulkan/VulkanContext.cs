using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanContext : IDisposable
{
    public Vk Vk;
    public Instance Instance;
    public VulkanInstance InstanceWrapper;
    public KhrSurface KhrSurface;
    public SurfaceKHR Surface;
    public PhysicalDevice PhysicalDevice;
    public Device Device;
    public Queue GraphicsQueue;
    public Queue PresentQueue;
    public QueueFamilyIndices QueueFamilyIndices;
    public SelectedPhysicalDevice SelectedDevice;
    public KhrSwapchain KhrSwapchain;

    public bool ValidationEnabled => InstanceWrapper?.ValidationEnabled ?? false;

    public void Init(EngineWindow window)
    {
        Vk = Vk.GetApi();

        InstanceWrapper = new VulkanInstance();
        InstanceWrapper.Create(Vk, window.GetRequiredInstanceExtensions());
        Instance = InstanceWrapper.Handle;

        Surface = window.CreateSurface(Instance);

        if (!Vk.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            throw new Exception("Расширение VK_KHR_surface недоступно.");
        }
        KhrSurface = khrSurface;

        var selector = new VulkanPhysicalDeviceSelector(Vk, Instance, KhrSurface, Surface);
        SelectedDevice = selector.GetDevice(new RequestedParameters());
        PhysicalDevice = SelectedDevice.Device;
        QueueFamilyIndices = SelectedDevice.QueueFamilyIndices;

        CreateLogicalDevice();

        if (!Vk.TryGetDeviceExtension(Instance, Device, out KhrSwapchain khrSwapchain))
        {
            throw new Exception("VK_KHR_swapchain расширение устройства недоступно.");
        }
        KhrSwapchain = khrSwapchain;
    }

    private void CreateLogicalDevice()
    {
        uint graphicsFamily = QueueFamilyIndices.GraphicsFamily!.Value;
        uint presentFamily = QueueFamilyIndices.PresentFamily!.Value;

        var uniqueFamilies = new List<uint> { graphicsFamily };
        if (presentFamily != graphicsFamily)
        {
            uniqueFamilies.Add(presentFamily);
        }

        float* priorities = stackalloc float[1];
        priorities[0] = 1.0f;

        var queueInfos = new DeviceQueueCreateInfo[uniqueFamilies.Count];
        for (int i = 0; i < uniqueFamilies.Count; i++)
        {
            queueInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = priorities
            };
        }

        IntPtr extensionName = Marshal.StringToHGlobalAnsi("VK_KHR_swapchain");
        byte** enabledExtensions = stackalloc byte*[1];
        enabledExtensions[0] = (byte*)extensionName;

        try
        {
            fixed (DeviceQueueCreateInfo* pQueueInfos = queueInfos)
            {
                var createInfo = new DeviceCreateInfo
                {
                    SType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = (uint)queueInfos.Length,
                    PQueueCreateInfos = pQueueInfos,
                    EnabledExtensionCount = 1,
                    PpEnabledExtensionNames = enabledExtensions
                };

                Result result = Vk.CreateDevice(PhysicalDevice, in createInfo, null, out Device device);
                if (result != Result.Success)
                {
                    throw new Exception($"vkCreateDevice failed: {result}");
                }
                Device = device;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extensionName);
        }

        Vk.GetDeviceQueue(Device, graphicsFamily, 0, out Queue graphicsQueue);
        GraphicsQueue = graphicsQueue;

        if (presentFamily == graphicsFamily)
        {
            PresentQueue = graphicsQueue;
        }
        else
        {
            Vk.GetDeviceQueue(Device, presentFamily, 0, out Queue presentQueue);
            PresentQueue = presentQueue;
        }
    }

    public void Dispose()
    {
        if (Device.Handle != 0)
        {
            Vk.DeviceWaitIdle(Device);
            Vk.DestroyDevice(Device, null);
            Device = default;
        }

        if (Surface.Handle != 0)
        {
            KhrSurface.DestroySurface(Instance, Surface, null);
            Surface = default;
        }

        InstanceWrapper?.Dispose();
        Instance = default;
    }
}
