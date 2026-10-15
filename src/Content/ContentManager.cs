using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Graphic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Content;

public sealed class ContentManager : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly Dictionary<(Type, string), object> _cache = new Dictionary<(Type, string), object>();
    private readonly Dictionary<Type, Func<ContentManager, string, object>> _loaders = new Dictionary<Type, Func<ContentManager, string, object>>();
    private string _root;

    private bool _disposed;

    public ContentManager(string rootDirectory = "Content")
    {
        RootDirectory = string.IsNullOrWhiteSpace(rootDirectory) ? "Content" : rootDirectory;

        RegisterLoader(typeof(Texture2D), LoadTexture);
        RegisterLoader(typeof(SpriteFont), LoadSpriteFont);
    }

    public ContentManager(GraphicsDevice graphicsDevice, string rootDirectory = "Content")
        : this(rootDirectory)
    {
        AttachDevice(graphicsDevice);
    }

    public GraphicsDevice GraphicsDevice { get; private set; }

    public void AttachDevice(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    public string RootDirectory
    {
        get => _root;
        set => _root = ResolveRoot(string.IsNullOrWhiteSpace(value) ? "Content" : value);
    }

    public string ContentRoot => _root;

    public void RegisterLoader(Type type, Func<ContentManager, string, object> loader)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        if (loader == null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        _loaders[type] = loader;
    }

    public T Load<T>(string assetName)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ContentManager));
        }
        if (string.IsNullOrWhiteSpace(assetName))
        {
            throw new ArgumentException("Asset name must not be empty.", nameof(assetName));
        }

        var key = (typeof(T), assetName);
        if (_cache.TryGetValue(key, out object cached))
        {
            return (T)cached;
        }

        if (!_loaders.TryGetValue(typeof(T), out Func<ContentManager, string, object> loader))
        {
            throw new InvalidOperationException($"No content loader is registered for type '{typeof(T).FullName}'.");
        }

        object asset = loader(this, assetName);
        if (asset == null)
        {
            throw new ContentLoadException($"Content loader for '{typeof(T).Name}' returned null for asset '{assetName}'.");
        }

        if (asset is not T typed)
        {
            throw new ContentLoadException(
                $"Content loader for '{assetName}' produced '{asset.GetType().Name}', expected '{typeof(T).Name}'.");
        }

        _cache[key] = typed;
        return typed;
    }

    public string ResolveExisting(string relative, params string[] extensions)
    {
        if (string.IsNullOrWhiteSpace(relative))
        {
            throw new ArgumentException("Asset path must not be empty.", nameof(relative));
        }

        string basePath = Path.IsPathRooted(relative) ? relative : Path.Combine(_root, relative);

        if (Path.HasExtension(basePath) && File.Exists(basePath))
        {
            return Path.GetFullPath(basePath);
        }

        string[] candidates = extensions == null || extensions.Length == 0
            ? new[] { string.Empty }
            : extensions;

        var tried = new List<string>();
        foreach (string extension in candidates)
        {
            string candidate = basePath + extension;
            tried.Add(candidate);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new FileNotFoundException(
            $"Content asset '{relative}' was not found. Tried: {string.Join(", ", tried)}");
    }

    private static object LoadTexture(ContentManager content, string assetName)
    {
        string path = content.ResolveExisting(assetName, ".png", ".bmp", ".jpg", ".jpeg", ".tga", ".gif");
        using Image<Rgba32> image = Image.Load<Rgba32>(path);
        return content.GraphicsDevice.Textures.CreateTexture(image);
    }

    private static object LoadSpriteFont(ContentManager content, string assetName)
    {
        string relative = assetName.Contains('/') || assetName.Contains('\\')
            ? assetName
            : "Fonts/" + assetName;

        string jsonPath = content.ResolveExisting(relative, ".json");
        FontDescription description = JsonSerializer.Deserialize<FontDescription>(
            File.ReadAllText(jsonPath), JsonOptions);

        if (description == null)
        {
            throw new ContentLoadException($"Font description '{jsonPath}' is empty.");
        }
        if (string.IsNullOrWhiteSpace(description.Ttf))
        {
            throw new ContentLoadException($"Font description '{jsonPath}' is missing the 'ttf' field.");
        }

        string ttfPath = content.ResolveExisting(description.Ttf, ".ttf", ".otf");
        byte[] ttf = File.ReadAllBytes(ttfPath);
        int[] codepoints = description.ExpandCodepoints();

        SpriteFont font = FontRasterizer.Rasterize(
            content.GraphicsDevice,
            ttf,
            description.Size,
            codepoints,
            description.Spacing);
        font.DefaultCharacter = '?';
        return font;
    }

    public void Unload()
    {
        foreach (object asset in _cache.Values)
        {
            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _cache.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Unload();
    }

    private static string ResolveRoot(string rootDirectory)
    {
        if (Directory.Exists(rootDirectory))
        {
            return Path.GetFullPath(rootDirectory);
        }

        string fromBase = Path.Combine(AppContext.BaseDirectory, rootDirectory);
        if (Directory.Exists(fromBase))
        {
            return Path.GetFullPath(fromBase);
        }

        return Path.GetFullPath(rootDirectory);
    }
}

public sealed class ContentLoadException : Exception
{
    public ContentLoadException(string message) : base(message)
    {
    }
}

public sealed class FontDescription
{
    public string Ttf { get; set; } = string.Empty;

    public float Size { get; set; } = 32f;

    public float Spacing { get; set; }

    public CharacterRegion[] Characters { get; set; } = Array.Empty<CharacterRegion>();

    public int[] ExpandCodepoints()
    {
        var codepoints = new List<int>();
        var seen = new HashSet<int>();

        foreach (CharacterRegion region in Characters)
        {
            int from = Math.Min(region.From, region.To);
            int to = Math.Max(region.From, region.To);
            for (int codepoint = from; codepoint <= to; codepoint++)
            {
                if (seen.Add(codepoint))
                {
                    codepoints.Add(codepoint);
                }
            }
        }

        return codepoints.ToArray();
    }
}

public sealed class CharacterRegion
{
    public int From { get; set; }

    public int To { get; set; }
}
