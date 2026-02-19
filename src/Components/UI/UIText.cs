using _2D_Engine_Base;
using Blockhand;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.UI;
public class UIText : UIRendererComponent
{
    public string Text;
    public Vector2 OffsetPosition;
    public Color TextColor = Color.Black;
    public float FontSize = 1f;
    public UIText() => ZIndex = 0;
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            spriteBatch.End();
            spriteBatch.Begin(transformMatrix: Matrix.CreateScale(FontSize, FontSize, 1f));
            if (Element.StretchSettings != null)
            {
                spriteBatch.DrawString(Game1.Instance.SpriteFont, Text, (Element.Transform.Position - Element.StretchSettings.TextureScale / 2 - Element.Transform.Size / 2 + OffsetPosition) / FontSize, TextColor);
            }
            else
            {
                spriteBatch.DrawString(Game1.Instance.SpriteFont, Text, (Element.Transform.Position - Element.Transform.Size / 2 + OffsetPosition) / FontSize, TextColor);
            }
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }
    }
    public void Init(string text, float fontSize = 1f, Vector2? offsetPosition = null)
    {
        FontSize = fontSize;
        Text = text;
        OffsetPosition = offsetPosition == null ? Vector2.Zero : offsetPosition.Value;
    }
}