using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphic;

public class ContentLoader
{
    #warning make it possible custom path
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source">Source in content folder</param>
    public Image<Rgba32> LoadImage(string source)
    {
        if (!File.Exists(source))
        {
            #warning Use debug.error!
            Console.WriteLine("Файл не существует.");
            return null;
        }

        try
        {
            return Image.Load<Rgba32>(source);
        }
        catch (Exception ex)
        {
            #warning Use debug.error!
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
        #warning Use debug.error!
        return null;
    }
}