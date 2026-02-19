using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.UI;

public interface IUIComponent
{
    UIElement Element { get; set; }
    int ZIndex { get; set; }
    int? id { get; set; }
    void Draw(SpriteBatch spriteBatch);
    void Update(GameTime gameTime);
    void LocalInit();
    void Destroying();
}
public abstract class UIRendererComponent : IUIComponent
{
    public UIElement Element { get; set; }
    public int ZIndex { get; set; } = 0;
    public int? id { get; set; }
    public abstract void Draw(SpriteBatch spriteBatch);
    public virtual void Update(GameTime gameTime) { }
    public virtual void LocalInit() { }
    public virtual void Destroying() { }
}
/// <summary>
/// Базовый класс для элементов пользовательского интерфейса (UI). Управляет компонентами, отрисовкой и обновлением.
/// </summary>
public class UIElement : ITransformNode
{
    private static List<UIElement> UIElements = new List<UIElement>();
    private static Stack<int> BusyIds = new Stack<int>();
    private static Texture2D CollidersTexture;
    private static StretchSettings ColliderStretchSettings;
    public static bool EnableColliders = false;
    public List<IUIComponent> UIComponents = new List<IUIComponent>();
    public Rectangle Rectangle => new Rectangle((int)Transform.Position.X, (int)Transform.Position.Y, (int)Transform.Size.X, (int)Transform.Size.Y);
    public Transform Transform { get; set; }
    public UIElement Parent;
    public Color Color;
    public StretchSettings StretchSettings;
    public Rectangle ColliderRectangle 
    {
        get
        {
            if (StretchSettings != null && StretchSettings.Rectangle != Rectangle.Empty)
            {
                return StretchSettings.Rectangle;
            }
            
            return new Rectangle(
                (int)(Transform.Position.X - Transform.Size.X / 2),
                (int)(Transform.Position.Y - Transform.Size.Y / 2),
                (int)Transform.Size.X,
                (int)Transform.Size.Y
            );
        }
    }

    /// <summary>
    /// Конструктор класса <see cref="UIElement"/>.
    /// </summary>
    /// <param name="position">Позиция элемента.</param>
    /// <param name="size">Размеры элемента.</param>
    /// <param name="color">Цвет элемента. Если значение равно null, используется белый цвет.</param>
    /// <param name="stretchSettings">Настройки растяжки. Если значение равно null, используются стандартные настройки.</param>
    public UIElement(Vector2 position, Vector2 size, Color? color = null, StretchSettings stretchSettings = null)
    {
        StretchSettings = stretchSettings;
        Color = color ?? Color.White;
        UIElements.Add(this);
        Transform = new Transform(position, size);
    }

    /// <summary>
    /// Добавляет компонент указанного типа к элементу.
    /// </summary>
    /// <typeparam name="T">Тип компонента, реализующий интерфейс <see cref="IUIComponent"/>.</typeparam>
    /// <returns>Добавленный компонент.</returns>
    public T AddComponent<T>() where T : IUIComponent, new()
    {
        T component = new T();
        component.Element = this;
        UIComponents.Add(component);
        UIComponents.OrderBy(e => e.ZIndex);
        component.LocalInit();
        return component;
    }

    /// <summary>
    /// Получает компонент указанного типа из списка компонентов.
    /// </summary>
    /// <typeparam name="T">Тип компонента, реализующий интерфейс <see cref="IUIComponent"/>.</typeparam>
    /// <returns>Найденный компонент или null, если компонент не найден.</returns>
    public T GetComponent<T>() where T : class, IUIComponent
    {
        foreach (var component in UIComponents)
        {
            if (component is T result)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Удаляет все компоненты указанного типа из списка.
    /// </summary>
    /// <typeparam name="T">Тип компонента, реализующий интерфейс <see cref="IUIComponent"/>.</typeparam>
    public void RemoveComponent<T>() where T : IUIComponent, new()
    {
        foreach (var component in UIComponents)
        {
            if (component is T result)
            {
                component.Destroying();
                UIComponents.Remove(result);
            }
        }
    }

    /// <summary>
    /// Назначает глобальный уникальный идентификатор компоненту.
    /// </summary>
    /// <param name="component">Компонент, которому назначается идентификатор.</param>
    /// <exception cref="Exception">Выбрасывается, если превышено максимальное количество компонентов.</exception>
    public void CreateGlobalComponentId(IUIComponent component)
    {
        for (int i = 0; i < 100000; i++)
        {
            if (!BusyIds.Contains(i))
            {
                BusyIds.Push(i);
                component.id = i;
                return;
            }
        }
        throw new Exception("Too many UI components");
    }

    /// <summary>
    /// Отрисовывает все элементы и их компоненты.
    /// </summary>
    /// <param name="spriteBatch">Объект для отрисовки.</param>
    public static void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        for (int j = 0; j < UIElements.Count; j++)
        {
            if (UIElements[j].Transform.Visible)
            {
                for (int i = 0; i < UIElements[j].UIComponents.Count; i++)
                {
                    UIElements[j].UIComponents[i].Draw(spriteBatch);
                }
            }
        }
        if (EnableColliders)
        {
            for (int i = 0; i < UIElements.Count; i++)
            {
                if (UIElements[i].Transform.Visible)
                {
                    StretchSettings.DrawStretched(spriteBatch, CollidersTexture, UIElements[i].ColliderRectangle, ColliderStretchSettings, null, false);
                }
            }
        }
        spriteBatch.End();
    }

    /// <summary>
    /// Обновляет все элементы и их компоненты.
    /// </summary>
    /// <param name="gameTime">Информация о времени игры.</param>
    public static void Update(GameTime gameTime)
    {
        for (int j = 0; j < UIElements.Count; j++)
        {
            for (int i = 0; i < UIElements[j].UIComponents.Count; i++)
            {
                UIElements[j].UIComponents[i].Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Инициализирует статические ресурсы для элементов UI.
    /// </summary>
    /// <param name="content">Менеджер контента для загрузки текстур.</param>
    public static void Load(ContentManager content)
    {
        CollidersTexture = content.Load<Texture2D>("UICollider");
        ColliderStretchSettings = new StretchSettings(new Vector2(8, 8), null, true, true);
    }
}

public class StretchSettings
{
    public Vector2 StretchPoint;
    public Vector2 TextureScale;
    public bool StretchHorizontal;
    public bool StretchVertical;
    public Rectangle Rectangle;

    public StretchSettings(Vector2 point, Vector2? textureScale = null, bool horizontal = true, bool vertical = false)
    {
        StretchPoint = point;
        TextureScale = textureScale ?? new Vector2(1, 1);
        StretchHorizontal = horizontal;
        StretchVertical = vertical;
    }

    public static void DrawStretched(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, StretchSettings stretch, Rectangle? source = null, bool centering = true)
    {
        Vector2 stretchPoint = stretch.StretchPoint;

        int texWidth = texture.Width;
        int texHeight = texture.Height;

        Vector2 posOffset = !centering ? Vector2.Zero : new Vector2(destination.Width / 2 + stretch.TextureScale.X / 2, destination.Height / 2 + stretch.TextureScale.Y / 2);

        if (!stretch.StretchHorizontal && !stretch.StretchVertical)
        {
            source ??= new Rectangle(0, 0, texWidth, texHeight);
            Rectangle dest = new Rectangle(
                destination.X - (int)stretch.TextureScale.X,
                destination.Y - (int)posOffset.Y,
                (int)(source.Value.Width + stretch.TextureScale.X),
                (int)(source.Value.Height + stretch.TextureScale.Y)
            );

            spriteBatch.Draw(texture, dest, source, Color.White);

            stretch.Rectangle = dest;
            return;
        }


        if (stretch.StretchHorizontal && stretch.StretchVertical)
        {
            source ??= new Rectangle(0, 0, texWidth, texHeight);

            int left = (int)stretch.StretchPoint.X;
            int right = source.Value.Width - (int)stretchPoint.X;
            int top = (int)stretch.StretchPoint.Y;
            int bottom = source.Value.Height - (int)stretchPoint.Y;

            int sourceTexHeight = source.Value.Height + source.Value.Y;
            int sourceTexWidth = source.Value.Width + source.Value.X;

            int centerWidth = source.Value.Width - left - right;
            int centerHeight = source.Value.Height - top - bottom;

            // 1. Углы 
            // Левый верхний
            spriteBatch.Draw(texture,
                new Rectangle(destination.X - (int)posOffset.X, destination.Y - (int)posOffset.Y, left + (int)stretch.TextureScale.X, top + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X, source.Value.Y, left, top), Color.White);

            // Правый верхний
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - right - (int)posOffset.X, destination.Y - (int)posOffset.Y, right + (int)stretch.TextureScale.X, top + (int)stretch.TextureScale.Y),
                new Rectangle(sourceTexWidth - right, source.Value.Y, right, top), Color.White);

            // Левый нижний
            spriteBatch.Draw(texture,
                new Rectangle(destination.X - (int)posOffset.X, destination.Bottom - bottom - (int)posOffset.Y, left + (int)stretch.TextureScale.X, bottom + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X, sourceTexHeight - bottom, left, bottom), Color.White);

            // Правый нижний
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - right - (int)posOffset.X, destination.Bottom - bottom - (int)posOffset.Y, right + (int)stretch.TextureScale.X, bottom + (int)stretch.TextureScale.Y),
                new Rectangle(sourceTexWidth - right, sourceTexHeight - bottom, right, bottom), Color.White);

            // 2. Стороны 
            // Верхняя сторона
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + left - (int)posOffset.X, destination.Y - (int)posOffset.Y,
                             destination.Width - left - right + (int)stretch.TextureScale.X, top + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X + left, source.Value.Y, centerWidth, top), Color.White);

            // Нижняя сторона
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + left - (int)posOffset.X, destination.Bottom - bottom - (int)posOffset.Y,
                             destination.Width - left - right + (int)stretch.TextureScale.X, bottom + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X + left, sourceTexWidth - bottom, centerWidth, bottom), Color.White);

            // Левая сторона
            spriteBatch.Draw(texture,
                new Rectangle(destination.X - (int)posOffset.X, destination.Y + top - (int)posOffset.Y,
                             left + (int)stretch.TextureScale.X, destination.Height - top - bottom + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X, top, source.Value.Y + left, centerHeight), Color.White);

            // Правая сторона
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - right - (int)posOffset.X, destination.Y + top - (int)posOffset.Y,
                             right + (int)stretch.TextureScale.X, destination.Height - top - bottom + (int)stretch.TextureScale.Y),
                new Rectangle(sourceTexWidth - right, source.Value.Y + top, right, centerHeight), Color.White);

            // 3. Центр 
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + left - (int)posOffset.X, destination.Y + top - (int)posOffset.Y,
                             destination.Width - left - right + (int)stretch.TextureScale.X, destination.Height - top - bottom + (int)stretch.TextureScale.Y),
                new Rectangle(source.Value.X + left, source.Value.Y + top, centerWidth, centerHeight), Color.White);

            stretch.Rectangle = new Rectangle(destination.X - (int)posOffset.X, destination.Y - (int)posOffset.Y,
             destination.Width + (int)stretch.TextureScale.X, top + destination.Height - bottom + (int)stretch.TextureScale.Y);
            return;
        }

        if (stretch.StretchHorizontal)
        {
            source ??= new Rectangle(0, 0, texWidth, texHeight);

            Rectangle sourceLeft = new Rectangle(
                source.Value.X, source.Value.Y,
                (int)stretchPoint.X,
                source.Value.Height
            );

            Rectangle destLeft = new Rectangle(
                destination.X - (int)stretch.TextureScale.X - (int)posOffset.X,
                destination.Y - (int)posOffset.Y,
                (int)(stretchPoint.X + stretch.TextureScale.X),
                (int)(source.Value.Height + stretch.TextureScale.Y)
            );

            spriteBatch.Draw(texture, destLeft, sourceLeft, Color.White);

            Rectangle sourceRight = new Rectangle(
                source.Value.X + (int)stretchPoint.X,
                source.Value.Y,
                source.Value.Width - (int)stretchPoint.X,
                source.Value.Height
            );

            Rectangle destRight = new Rectangle(
                destination.Right - (int)stretchPoint.Y + (int)stretch.TextureScale.X - (int)posOffset.X,
                destination.Y - (int)posOffset.Y,
                (int)(stretchPoint.X + stretch.TextureScale.X),
                (int)(source.Value.Height + stretch.TextureScale.Y)
            );

            spriteBatch.Draw(texture, destRight, sourceRight, Color.White);


            Rectangle sourceCenterH = new Rectangle(
                source.Value.X + (int)stretchPoint.X, source.Value.Y,
                1, source.Value.Height
            );

            Rectangle destCenterH = new Rectangle(
                destination.X + (int)stretchPoint.X - (int)posOffset.X,
                destination.Y - (int)posOffset.Y,
                destination.Width - source.Value.Width + (int)stretch.TextureScale.X,
                source.Value.Height + (int)stretch.TextureScale.Y
            );

            spriteBatch.Draw(texture, destCenterH, sourceCenterH, Color.White);

            stretch.Rectangle = new Rectangle(destination.X - (int)stretch.TextureScale.X - (int)posOffset.X,
             destination.Y - (int)posOffset.Y,
             destination.Width - source.Value.Width + (int)stretchPoint.X * 2 + (int)stretch.TextureScale.X * 3,
             source.Value.Height + (int)stretch.TextureScale.Y);
            return;
        }

        if (stretch.StretchVertical)
        {
            source ??= new Rectangle(0, 0, texWidth, texHeight);

            Rectangle sourceUp = new Rectangle(
                source.Value.X, source.Value.Y,
                source.Value.Width,
                (int)stretchPoint.Y
            );

            Rectangle destUp = new Rectangle(
                destination.X - (int)posOffset.X,
                destination.Y - (int)stretch.TextureScale.Y - (int)posOffset.Y,
                (int)(source.Value.Width + stretch.TextureScale.X),
                (int)(stretchPoint.Y + stretch.TextureScale.Y)
            );

            spriteBatch.Draw(texture, destUp, sourceUp, Color.White);

            Rectangle sourceDown = new Rectangle(
                source.Value.X,
                source.Value.Y + (int)stretchPoint.Y,
                source.Value.Width,
                source.Value.Height - (int)stretchPoint.Y
            );

            Rectangle destDown = new Rectangle(
                destination.X - (int)posOffset.X,
                destination.Bottom - (int)stretchPoint.Y - (int)posOffset.Y + (int)stretch.TextureScale.Y,
                (int)(source.Value.Width + stretch.TextureScale.X),
                (int)(stretchPoint.Y + stretch.TextureScale.Y)
            );

            spriteBatch.Draw(texture, destDown, sourceDown, Color.White);


            Rectangle sourceCenterH = new Rectangle(
                source.Value.X, source.Value.Y + (int)stretchPoint.Y,
                source.Value.Width, 1
            );

            Rectangle destCenterH = new Rectangle(
                destination.X - (int)posOffset.X,
                destination.Y + (int)stretchPoint.Y - (int)posOffset.Y,
                source.Value.Width + (int)stretch.TextureScale.X,
                destination.Height - source.Value.Height + (int)stretch.TextureScale.Y
            );

            spriteBatch.Draw(texture, destCenterH, sourceCenterH, Color.White);
            stretch.Rectangle = new Rectangle(destination.X - (int)posOffset.X, destination.Y - (int)stretch.TextureScale.Y - (int)posOffset.Y,
             source.Value.Width + (int)stretch.TextureScale.X,
             destination.Y - (int)posOffset.Y - destination.Bottom - (int)stretchPoint.Y - (int)posOffset.Y);
        }
    }
}