using System;
using System.Globalization;
using Engine.Graphic;
using Engine.Graphic.Vulkan;
using Engine.Input;
using Silk.NET.Input;

namespace Engine;

public static class GameHost
{
    private const int MaxInstances = 16;
    private const int FramesInFlight = 2;

    public static void Run(Game game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        int width = 1280;
        int height = 720;
        bool fullscreen = false;

        GraphicsDeviceManager manager = game.DeviceManager;
        if (manager != null)
        {
            if (manager.PreferredBackBufferWidth > 0)
            {
                width = manager.PreferredBackBufferWidth;
            }
            if (manager.PreferredBackBufferHeight > 0)
            {
                height = manager.PreferredBackBufferHeight;
            }
            fullscreen = manager.IsFullScreen;
        }

        bool windowed = Environment.GetEnvironmentVariable("ENGINE_WINDOWED") == "1";
        if (windowed)
        {
            fullscreen = false;
        }

        bool resizeTest = Environment.GetEnvironmentVariable("ENGINE_RESIZE_TEST") == "1";

        var window = new EngineWindow(game.GetType().Name, width, height);
        if (fullscreen)
        {
            window.SetFullscreen(true);
        }

        game.Window = new GameWindow(window);

        double exitAfter = ParseExitAfter();

        VulkanContext context = null;
        Renderer2D renderer = null;
        GraphicsDevice graphicsDevice = null;
        IInputContext input = null;
        var time = new Time();
        var gameTime = new GameTime();
        var camera = new Camera2D();
        double elapsed = 0.0;
        int lastResizePhase = -1;
        bool closing = false;

        window.Load += () =>
        {
            context = new VulkanContext();
            context.Init(window);

            renderer = new Renderer2D(context, window, MaxInstances, FramesInFlight);
            graphicsDevice = new GraphicsDevice(context, renderer);

            game.GraphicsDevice = graphicsDevice;
            game.Content.AttachDevice(graphicsDevice);

            input = window.Window.CreateInput();
            Keyboard.Bind(input.Keyboards.Count > 0 ? input.Keyboards[0] : null);
            Mouse.Bind(input.Mice.Count > 0 ? input.Mice[0] : null);
            Mouse.SetCursorVisible(game.IsMouseVisible);

            game.DoInitialize();
            game.DoLoadContent();

            Console.WriteLine(
                $"GPU: {context.SelectedDevice.DeviceName}; validation: {(context.ValidationEnabled ? "on" : "off")}; " +
                $"backbuffer: {renderer.BackbufferWidth}x{renderer.BackbufferHeight}");
        };

        window.Update += _ =>
        {
            if (graphicsDevice == null)
            {
                return;
            }

            time.Update();
            gameTime.Update(time);
            Mouse.SetCursorVisible(game.IsMouseVisible);
            game.DoUpdate(gameTime);

            if (game.ExitRequested && !closing)
            {
                closing = true;
                window.Close();
            }
        };

        window.Render += delta =>
        {
            if (graphicsDevice == null || closing)
            {
                return;
            }

            renderer.ClearColor = graphicsDevice.ClearColor;

            int w = renderer.BackbufferWidth;
            int h = renderer.BackbufferHeight;
            camera.Position = new System.Numerics.Vector2(w * 0.5f, h * 0.5f);
            camera.Zoom = 1.0f;

            game.DoDraw(gameTime);

            renderer.DrawFrame(camera, ReadOnlySpan<Instance2D>.Empty, graphicsDevice.ActiveSpriteBatch);

            elapsed += delta;
            if (resizeTest)
            {
                int phase = (int)(elapsed / 2.0);
                if (phase != lastResizePhase)
                {
                    lastResizePhase = phase;
                    window.SetSize(phase % 2 == 0 ? 1024 : 1400, phase % 2 == 0 ? 640 : 900);
                }
            }
            if (exitAfter > 0.0 && elapsed >= exitAfter)
            {
                closing = true;
                window.Close();
            }
        };

        window.Closing += () =>
        {
            if (closing)
            {
                return;
            }
            closing = true;

            context?.Vk.DeviceWaitIdle(context.Device);
            input?.Dispose();
            graphicsDevice?.ActiveSpriteBatch?.Dispose();
            game.Content?.Dispose();
            renderer?.Dispose();
            context?.Dispose();
        };

        window.Run();
    }

    private static double ParseExitAfter()
    {
        return double.TryParse(
            Environment.GetEnvironmentVariable("ENGINE_EXIT_AFTER"),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double seconds)
            ? seconds
            : 0.0;
    }
}
