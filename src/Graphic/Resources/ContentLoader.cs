using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public class ContentLoader
{
    private readonly string _root;

    public ContentLoader(string contentRoot = "Content")
    {
        _root = Path.GetFullPath(contentRoot);
    }

    public string ContentRoot => _root;

    public string ResolvePath(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source path must not be empty.", nameof(source));
        }

        string candidate = Path.IsPathRooted(source) ? source : Path.Combine(_root, source);
        string full = Path.GetFullPath(candidate);

        string rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Path '{source}' resolves outside the content root '{_root}'.");
        }

        return full;
    }

    /// <summary>
    /// Loads an image by a path relative to the content root.
    /// </summary>
    /// <param name="source">Source relative to the content folder.</param>
    public Image<Rgba32> LoadImage(string source)
    {
        string path = ResolvePath(source);
        if (!File.Exists(path))
        {
            Console.WriteLine("Файл не существует.");
            return null;
        }

        try
        {
            return Image.Load<Rgba32>(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке изображения: {ex.Message}");
            return null;
        }
    }

    public Texture2D LoadTexture(string source)
    {
        Image<Rgba32> image = LoadImage(source);
        if (image != null)
        {
            return new Texture2D(image);
        }
        return null;
    }
}
