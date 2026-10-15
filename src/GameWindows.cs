using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Engine;

public class EngineWindow
{
    private IWindow _window;

    public EngineWindow(string name)
    {
        WindowOptions options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = name,
            VSync = false
        };
        _window = Silk.NET.Windowing.Window.Create(options);

        _window.Load += () => Load?.Invoke();
        _window.Update += delta => Update?.Invoke(delta);
        _window.Render += delta => Render?.Invoke(delta);
        _window.FramebufferResize += size => FramebufferResize?.Invoke(size);
        _window.Closing += OnClosing;
    }

    public IWindow Window => _window;

    public int FramebufferWidth => _window.FramebufferSize.X;

    public int FramebufferHeight => _window.FramebufferSize.Y;

    public bool IsClosing => _window.IsClosing;

    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    public void Run()
    {
        _window.Run();
    }

    public void DoEvents()
    {
        _window.DoEvents();
    }

    public void Close()
    {
        _window.Close();
    }

    public unsafe SurfaceKHR CreateSurface(Instance instance)
    {
        return _window.VkSurface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    }

    public unsafe string[] GetRequiredInstanceExtensions()
    {
        byte** extensions = _window.VkSurface.GetRequiredExtensions(out uint count);
        string[] result = new string[count];
        for (uint i = 0; i < count; i++)
        {
            result[i] = Marshal.PtrToStringAnsi((IntPtr)extensions[i]) ?? string.Empty;
        }
        return result;
    }

    private void OnClosing()
    {
        Closing?.Invoke();
    }

    public event Action<double> Update;
    public event Action<double> Render;
    public event Action Load;
    public event Action Closing;
    public event Action<Vector2D<int>> FramebufferResize;
}
