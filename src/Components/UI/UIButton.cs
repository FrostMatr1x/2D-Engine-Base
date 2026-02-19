using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Engine.UI;
public class UIButton : UIRendererComponent
{
    public Action<object[]> action;
    public UIButton() => ZIndex = 2;
    public void CheckButton()
    {
        if (Element.Transform.Visible)
        {
            Rectangle mouseRectangle = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 1, 1);
            if (Element.Transform.Intersects(mouseRectangle))
            {
                action?.Invoke(null);
            }
        }
    }
    public override void LocalInit()
    {
        Element.CreateGlobalComponentId(this);
        if (id.HasValue)
        {
            InputHelper.InputMouse.AddButtonEvent(MouseButton.LeftButton, _ => CheckButton(), "UIButtomID: " + id, true);
        }
        else
        {
            Console.WriteLine("Error: No UI component id");
        }
    }
    public override void Destroying()
    {
        if (id.HasValue)
        {
            InputHelper.InputMouse.RemoveButtonEvent(MouseButton.LeftButton, "UIButtomID: " + id);
        }
        else
        {
            Console.WriteLine("Error: No UI component id");
        }
    }
    public override void Draw(SpriteBatch spriteBatch) { }
}