using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.UI;
public class UIImage : UIRendererComponent
{
    public Texture2D Image;
    public Rectangle? rectangle;
    public UIImage() => ZIndex = 1;
    public Color Color = Color.White;
    public override void Draw(SpriteBatch spriteBatch)
    {
        Color finalColor = Color.Lerp(Color, Element.Color, 0.5f);
        if (Image != null)
        {
            if (Element.StretchSettings != null)
            {
                if (rectangle != Rectangle.Empty)
                {
                    StretchSettings.DrawStretched(spriteBatch, Image, Element.Rectangle, Element.StretchSettings, rectangle);
                }
                else
                {
                    StretchSettings.DrawStretched(spriteBatch, Image, Element.Rectangle, Element.StretchSettings);
                }
            }
            else
            {
                if (rectangle != Rectangle.Empty)
                {
                    spriteBatch.Draw(Image, Element.ColliderRectangle, rectangle, finalColor);
                }
                else
                {
                    spriteBatch.Draw(Image, Element.ColliderRectangle, finalColor);
                }
            }
        }
    }
    public void Init(Texture2D image, int ZIndex = 1)
    {
        Image = image;
        this.ZIndex = ZIndex;
    }
    public void Init(Texture2D spriteMap, Rectangle imageRect, int ZIndex = 1)
    {
        this.ZIndex = ZIndex;
        Image = spriteMap;
        rectangle = imageRect;
    }
}