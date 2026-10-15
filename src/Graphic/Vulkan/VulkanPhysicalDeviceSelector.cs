using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Engine.Graphic.Vulkan;

/// <summary>
/// Информация о поддержке swapchain конкретным устройством для конкретной surface.
/// </summary>
public struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;

    public readonly bool IsAdequate => Formats.Length > 0 && PresentModes.Length > 0;
}

/// <summary>
/// Результат работы селектора, включая всю полезную информацию.
/// </summary>
public struct SelectedPhysicalDevice
{
    public PhysicalDevice Device;
    public QueueFamilyIndices QueueFamilyIndices;
    public SwapChainSupportDetails SwapChainSupport;
    public string DeviceName;
}

public class RequestedParameters
{
    public QueueFlags RequiredQueueFlags = QueueFlags.GraphicsBit | QueueFlags.TransferBit;
    public string[] RequiredExtensionNames = ["VK_KHR_swapchain"];
    public bool RequireSamplerAnisotropy = false;
}

public class VulkanPhysicalDeviceSelector
{
    private readonly Vk _vk;
    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surface;

    private SelectedPhysicalDevice? _selected;

    public VulkanPhysicalDeviceSelector(Vk vk, Instance instance, KhrSurface khrSurface, SurfaceKHR surface)
    {
        _vk = vk;
        _instance = instance;
        _khrSurface = khrSurface;
        _surface = surface;
    }

    /// <summary>
    /// Главный метод, который перебирает все физические устройства, отсеивает неподходящие, оценивает оставшиеся и возвращает лучшее.
    /// </summary>
    public SelectedPhysicalDevice GetDevice(RequestedParameters requestedParameters)
    {
        if (_selected.HasValue)
        {
            return _selected.Value;
        }

        var physicalDevices = _vk.GetPhysicalDevices(_instance);

        if (physicalDevices.Count == 0)
        {
            throw new Exception("Не найдено ни одной GPU с поддержкой Vulkan.");
        }

        SelectedPhysicalDevice? best = null;
        int bestScore = -1;

        foreach (var device in physicalDevices)
        {
            if (!TryGetSuitableDeviceInfo(device, requestedParameters, out var candidate))
            {
                continue;
            }

            int score = RateDevice(device);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (!best.HasValue)
        {
            throw new Exception("Ни одна из доступных GPU не подходит под требования движка.");
        }

        _selected = best;
        return best.Value;
    }

    /// <summary>
    /// Проверяет одно устройство на пригодность.
    /// </summary>
    private unsafe bool TryGetSuitableDeviceInfo(PhysicalDevice device, RequestedParameters requestedParameters, out SelectedPhysicalDevice result)
    {
        result = default;

        var queueFamilyIndices = FindQueueFamilies(device, requestedParameters.RequiredQueueFlags);
        if (!queueFamilyIndices.IsComplete)
        {
            return false;
        }

        if (!CheckExtensionSupport(device, requestedParameters.RequiredExtensionNames))
        {
            return false;
        }

        var swapChainSupport = QuerySwapChainSupport(device);
        if (!swapChainSupport.IsAdequate)
        {
            return false;
        }

        if (requestedParameters.RequireSamplerAnisotropy)
        {
            _vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures features);
            if (!features.SamplerAnisotropy)
            {
                return false;
            }
        }

        _vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties properties);
        string deviceName = Marshal.PtrToStringAnsi((IntPtr)properties.DeviceName) ?? "Unknown device";

        result = new SelectedPhysicalDevice
        {
            Device = device,
            QueueFamilyIndices = queueFamilyIndices,
            SwapChainSupport = swapChainSupport,
            DeviceName = deviceName
        };
        return true;
    }

    /// <summary>
    /// Проверка устройства на умение present на surface.
    /// </summary>
    private unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device, QueueFlags requiredQueueFlags)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, pQueueFamilies);
        }

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & requiredQueueFlags) == requiredQueueFlags)
            {
                indices.GraphicsFamily = i;
            }

            _khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out Bool32 presentSupported);
            if (presentSupported)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete)
            {
                break;
            }
        }

        return indices;
    }

    /// <summary>
    /// Проверяем, что все обязательные расширения поддерживаются устройством.
    /// </summary>
    private unsafe bool CheckExtensionSupport(PhysicalDevice device, string[] requiredExtensionNames)
    {
        uint extensionCount = 0;
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pAvailableExtensions = availableExtensions)
        {
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, pAvailableExtensions);
        }

        var requiredSet = new HashSet<string>(requiredExtensionNames);

        foreach (var extension in availableExtensions)
        {
            var name = Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName);
            if (name != null)
            {
                requiredSet.Remove(name);
            }

            if (requiredSet.Count == 0)
            {
                break;
            }
        }

        return requiredSet.Count == 0;
    }

    /// <summary>
    /// Получение capabilities/formats/presentModes для surface на устройстве.
    /// </summary>
    private unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
    {
        var details = new SwapChainSupportDetails();

        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, _surface, out details.Capabilities);

        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, ref formatCount, null);
        details.Formats = new SurfaceFormatKHR[formatCount];
        if (formatCount != 0)
        {
            fixed (SurfaceFormatKHR* pFormats = details.Formats)
            {
                _khrSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, ref formatCount, pFormats);
            }
        }

        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, ref presentModeCount, null);
        details.PresentModes = new PresentModeKHR[presentModeCount];
        if (presentModeCount != 0)
        {
            fixed (PresentModeKHR* pPresentModes = details.PresentModes)
            {
                _khrSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, ref presentModeCount, pPresentModes);
            }
        }

        return details;
    }

    /// <summary>
    /// Выдача значений для устройств.
    /// </summary>
    private int RateDevice(PhysicalDevice device)
    {
        _vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties properties);

        int score = properties.DeviceType switch
        {
            PhysicalDeviceType.DiscreteGpu => 1000,
            PhysicalDeviceType.IntegratedGpu => 100,
            PhysicalDeviceType.VirtualGpu => 50,
            PhysicalDeviceType.Cpu => 10,
            _ => 0
        };

        score += (int)(properties.Limits.MaxImageDimension2D / 1000);

        return score;
    }

    // Это самое тяжелое и скучное, что я делал :(
}