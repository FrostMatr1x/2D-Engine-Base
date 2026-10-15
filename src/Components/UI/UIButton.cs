using System.Numerics;
using Engine.Graphic;
using Engine.Input;
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
        id = Element.GetGlobalComponentId();
        if (id.HasValue)
        {
            InputHelper.InputMouse.AddButtonEvent(MouseButton.LeftButton, _ => CheckButton(), "UIButtomID: " + id, true);
        }
        else
        {
            Debug.Error("No UI component id");
        }
    }

    public override void Destroying()
    {
        if (id.HasValue)
        {
            InputHelper.InputMouse.RemoveButtonEvent(MouseButton.LeftButton, "UIButtomID: " + id);
            Element.DestroyGlobalComponentId(id.Value);
        }
        else
        {
            Debug.Error("No UI component id");
        }
    }

    public override void Draw(SpriteBatch spriteBatch) { }
}