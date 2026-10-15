using System.Numerics;
using Engine.Graphic;

namespace Engine.UI;
public class UIText : UIRendererComponent
{
    private static SpriteFont _basicSpriteFont;
    private static bool _isInit = false;

    public SpriteFont SpriteFont;
    public Color TextColor = Color.Black;
    public Vector2 OffsetPosition;
    public float FontSize = 1f;
    public string Text;
    
    public UIText() => ZIndex = 0;

    public static void Init(SpriteFont spriteFont)
    {
        if (_isInit) return;

        _basicSpriteFont = spriteFont;
        _isInit = true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            spriteBatch.End();
            spriteBatch.Begin(transformMatrix: Matrix.CreateScale(FontSize, FontSize, 1f));
            if (Element.StretchSettings != null)
            {
                spriteBatch.DrawString(SpriteFont ?? _basicSpriteFont, Text, (Element.Transform.Position - Element.StretchSettings.TextureScale / 2 - Element.Transform.Size / 2 + OffsetPosition) / FontSize, TextColor);
            }
            else
            {
                spriteBatch.DrawString(SpriteFont ?? _basicSpriteFont, Text, (Element.Transform.Position - Element.Transform.Size / 2 + OffsetPosition) / FontSize, TextColor);
            }
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }
    }

    public void Init(string text, float fontSize = 1f, Vector2? offsetPosition = null, SpriteFont spriteFont = null)
    {
        FontSize = fontSize;
        Text = text;
        OffsetPosition = offsetPosition == null ? Vector2.Zero : offsetPosition.Value;
        SpriteFont = spriteFont == null ? _basicSpriteFont : spriteFont;
    }
}