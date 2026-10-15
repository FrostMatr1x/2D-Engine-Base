using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanInstance : IDisposable
{
    private const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";
    private const string DebugUtilsExtensionName = "VK_EXT_debug_utils";

    private static DebugUtilsMessengerCallbackFunctionEXT _callback;

    private Vk _vk;
    private ExtDebugUtils _debugUtils;
    private DebugUtilsMessengerEXT _messenger;
    private Instance _instance;
    private bool _validation;
    private bool _hasMessenger;

    public Instance Handle => _instance;

    public bool ValidationEnabled => _validation;

    public void Create(Vk vk, string[] requiredExtensions)
    {
        _vk = vk;
        _validation = IsLayerAvailable(ValidationLayerName);

        var extensions = new List<string>(requiredExtensions);
        if (_validation)
        {
            extensions.Add(DebugUtilsExtensionName);
        }

        var handles = new List<GCHandle>();
        try
        {
            byte* appName = PinUtf8("2D Engine", handles);
            byte* engineName = PinUtf8("Engine", handles);
            byte*[] extensionPointers = PinUtf8Array(extensions.ToArray(), handles);
            byte*[] layerPointers = _validation ? PinUtf8Array(new[] { ValidationLayerName }, handles) : null;

            fixed (byte** ppExtensions = extensionPointers)
            fixed (byte** ppLayers = layerPointers)
            {
                var applicationInfo = new ApplicationInfo
                {
                    SType = StructureType.ApplicationInfo,
                    PApplicationName = appName,
                    ApplicationVersion = 1,
                    PEngineName = engineName,
                    EngineVersion = 1,
                    ApiVersion = Vk.Version13
                };

                var createInfo = new InstanceCreateInfo
                {
                    SType = StructureType.InstanceCreateInfo,
                    PApplicationInfo = &applicationInfo,
                    EnabledExtensionCount = (uint)extensions.Count,
                    PpEnabledExtensionNames = ppExtensions,
                    EnabledLayerCount = _validation ? 1u : 0u,
                    PpEnabledLayerNames = _validation ? ppLayers : null
                };

                Result result = _vk.CreateInstance(in createInfo, null, out _instance);
                if (result != Result.Success)
                {
                    throw new Exception($"vkCreateInstance failed: {result}");
                }
            }
        }
        finally
        {
            foreach (GCHandle handle in handles)
            {
                handle.Free();
            }
        }

        if (_validation)
        {
            CreateDebugMessenger();
        }
    }

    private bool IsLayerAvailable(string name)
    {
        uint count = 0;
        if (_vk.EnumerateInstanceLayerProperties(ref count, null) != Result.Success || count == 0)
        {
            return false;
        }

        var properties = new LayerProperties[count];
        fixed (LayerProperties* pProperties = properties)
        {
            if (_vk.EnumerateInstanceLayerProperties(ref count, pProperties) != Result.Success)
            {
                return false;
            }
        }

        foreach (LayerProperties property in properties)
        {
            string layerName = Marshal.PtrToStringAnsi((IntPtr)property.LayerName);
            if (layerName == name)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateDebugMessenger()
    {
        if (!_vk.TryGetInstanceExtension(_instance, out _debugUtils))
        {
            _validation = false;
            return;
        }

        _callback = DebugCallback;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = _callback
        };

        if (_debugUtils.CreateDebugUtilsMessenger(_instance, in createInfo, null, out _messenger) == Result.Success)
        {
            _hasMessenger = true;
        }
        else
        {
            _validation = false;
        }
    }

    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        string message = pCallbackData != null && pCallbackData->PMessage != null
            ? Marshal.PtrToStringAnsi((IntPtr)pCallbackData->PMessage)
            : "<null>";

        string level = messageSeverity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)
            ? "ERROR"
            : messageSeverity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.WarningBitExt) ? "WARN" : "INFO";

        Console.Error.WriteLine($"[Vulkan {level}] {message}");
        return 0;
    }

    private static byte* PinUtf8(string value, List<GCHandle> handles)
    {
        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(value) + 1];
        Encoding.UTF8.GetBytes(value, bytes);
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        handles.Add(handle);
        return (byte*)handle.AddrOfPinnedObject();
    }

    private static byte*[] PinUtf8Array(string[] values, List<GCHandle> handles)
    {
        byte*[] pointers = new byte*[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            pointers[i] = PinUtf8(values[i], handles);
        }
        return pointers;
    }

    public void Dispose()
    {
        if (_hasMessenger)
        {
            _debugUtils.DestroyDebugUtilsMessenger(_instance, _messenger, null);
            _hasMessenger = false;
        }

        if (_instance.Handle != 0)
        {
            _vk.DestroyInstance(_instance, null);
            _instance = default;
        }
    }
}
