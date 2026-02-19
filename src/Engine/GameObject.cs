using System;
using System.Collections.Generic;
using System.Linq;
using _2D_Engine_Base;
using Blockhand;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine;

public interface IComponent
{
    public GameObject GameObject { get; set; }
    public void Update(GameTime gameTime) { }
    public void Init() { }
}

public interface IDrawableComponent
{
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}

/// <summary>
/// Представляет базовый игровой объект, содержащий Transform, текстуру, компоненты и систему управления жизненным циклом.
/// </summary>
public class GameObject : ITransformNode
{
    private static Dictionary<int, GameObject> _gameObjects = new Dictionary<int, GameObject>();
    private static Stack<int> _freeIds = new Stack<int>();
    private static int _nextId = 0;
    private int _objectId;

    /// <summary>
    /// Трансформация объекта (позиция, размер, родитель и т.д.).
    /// </summary>
    public Transform Transform
    {
        get => IsDestroyed ? null : _transform;
        set
        {
            if (IsDestroyed) { return; }
            _transform = value;
        }
    }
    private Transform _transform;

    /// <summary>
    /// Текстура, отображаемая для объекта.
    /// </summary>
    public Texture2D Texture
    {
        get => IsDestroyed ? null : _texture;
        set
        {
            if (IsDestroyed) { return; }
            _texture = value;
        }
    }
    private Texture2D _texture;

    /// <summary>
    /// Флаг, указывающий, должна ли воспроизводиться анимация.
    /// </summary>
    public bool PlayAnimation
    {
        get => IsDestroyed ? false : _playAnimation;
        set
        {
            if (IsDestroyed) { return; }
            _playAnimation = value;
        }
    }
    private bool _playAnimation;

    /// <summary>
    /// Прямоугольник текстуры, используемый для отрисовки.
    /// </summary>
    public Rectangle TextureRectangle
    {
        get => IsDestroyed ? Rectangle.Empty : _rectangle;
        set
        {
            if (IsDestroyed) { return; }
            _rectangle = value;
        }
    }
    private Rectangle _rectangle;

    /// <summary>
    /// Флаг отражения текстуры по горизонтали.
    /// </summary>
    public SpriteEffects TextureFlip
    {
        get => IsDestroyed ? SpriteEffects.None : _textureFlip;
        set
        {
            if (IsDestroyed) { return; }
            _textureFlip = value;
        }
    }
    private SpriteEffects _textureFlip;

    /// <summary>
    /// Цвет объекта (используется при отрисовке).
    /// </summary>
    public Color Color
    {
        get => IsDestroyed ? Color.White : _color;
        set
        {
            if (IsDestroyed) { return; }
            _color = value;
        }
    }
    private Color _color;

    /// <summary>
    /// Компонент собственной отрисовки, связанный с объектом.
    /// </summary>
    public IDrawableComponent CustomDraw
    {
        get => IsDestroyed ? null : _customDraw;
        set
        {
            if (IsDestroyed) { return; }
            _customDraw = value;
        }
    }
    private IDrawableComponent _customDraw;

    /// <summary>
    /// Компонент коллизии, связанный с объектом.
    /// </summary>
    public Collider ColliderComponent => _cachedCollider;
    private Collider _cachedCollider;

    /// <summary>
    /// Список компонентов, прикреплённых к объекту.
    /// </summary>
    public List<IComponent> Components { get; private set; } = new List<IComponent>();

    /// <summary>
    /// Флаг, указывающий, был ли объект уничтожен.
    /// </summary>
    public bool IsDestroyed { private set; get; } = false;


    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GameObject"/> с текстурой и прямоугольником.
    /// </summary>
    /// <param name="position">Позиция объекта в мире.</param>
    /// <param name="spriteList">Текстура, содержащая спрайты.</param>
    /// <param name="textuerRectangle">Прямоугольник части текстуры для отображения.</param>
    /// <param name="color">Цвет отрисовки. Если null — белый.</param>
    /// <param name="textureFlip">Флаг отражения по горизонтали.</param>
    /// <param name="visible">Видимость объекта.</param>
    public GameObject(Vector2 position, Texture2D spriteList, Rectangle textuerRectangle, Color? color, SpriteEffects textureFlip = SpriteEffects.None, bool visible = true)
    {
        if (spriteList != null)
        {
            _objectId = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
            TextureRectangle = textuerRectangle;
            Color = color ?? Color.White;
            Texture = spriteList;
            TextureFlip = textureFlip;
            Transform = new Transform(position, new Vector2(1, 1));
            Transform.Visible = visible;

            _gameObjects.Add(_objectId, this);
        }
        else
        {
            Console.WriteLine("Error: SpriteList is null");
        }
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GameObject"/> с полной текстурой.
    /// </summary>
    /// <param name="position">Позиция объекта в мире.</param>
    /// <param name="displayTexture">Текстура для отображения.</param>
    /// <param name="color">Цвет отрисовки. Если null — белый.</param>
    /// <param name="textureFlip">Флаг отражения по горизонтали.</param>
    /// <param name="visible">Видимость объекта.</param>
    public GameObject(Vector2 position, Texture2D displayTexture, Color? color, SpriteEffects textureFlip = SpriteEffects.None, bool visible = true)
    {
        if (displayTexture != null)
        {
            _objectId = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
            Texture = displayTexture;
            TextureRectangle = new Rectangle(0, 0, displayTexture.Width, displayTexture.Height);
            Color = color ?? Color.White;
            TextureFlip = textureFlip;
            Transform = new Transform(position, new Vector2(1, 1));
            Transform.Visible = visible;

            _gameObjects.Add(_objectId, this);
        }
        else
        {
            Console.WriteLine("Error: DisplayTexture is null");
        }
    }

    /// <summary>
    /// Белый пиксель, используемый для отрисовки коллизий.
    /// </summary>
    private static Texture2D pixel;

    /// <summary>
    /// Добавляет компонент к объекту.
    /// </summary>
    /// <typeparam name="T">Тип компонента, реализующего <see cref="IComponent"/>.</typeparam>
    /// <returns>Добавленный компонент.</returns>
    public T AddComponent<T>() where T : IComponent, new()
    {
        T component = new T();
        component.GameObject = this;
        Components.Add(component);
        component.Init();

        if (component is Collider collider)
        {
            _cachedCollider = collider;
        }

        if (component is IDrawableComponent drawableComponent)
        {
            if (CustomDraw == null)
            {
                CustomDraw = drawableComponent;
            }
            else
            {
                Console.WriteLine("Error: CustomDraw is already set");
            }
        }

        return component;
    }

    /// <summary>
    /// Получает компонент указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    /// <returns>Найденный компонент или null, если не найден.</returns>
    public T GetComponent<T>() where T : class, IComponent
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T component)
            {
                return component;
            }
        }
        return null;
    }

    /// <summary>
    /// Удаляет компонент указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    public void RemoveComponent<T>() where T : class, IComponent
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T)
            {
                Components.RemoveAt(i);
                if (Components[i] is Collider)
                {
                    _cachedCollider = null;
                }
                return;
            }
        }
    }

    /// <summary>
    /// Уничтожает объект, удаляя его из системы и освобождая идентификатор.
    /// </summary>
    public void Destroy()
    {
        _gameObjects.Remove(_objectId);
        _freeIds.Push(_objectId);
        IsDestroyed = true;
    }

    /// <summary>
    /// Инициализирует статические ресурсы (белый пиксель).
    /// </summary>
    /// <param name="graphicsDevice">Графическое устройство.</param>
    public static void Init(GraphicsDevice graphicsDevice)
    {
        pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Отрисовывает все видимые объекты в порядке приоритета.
    /// </summary>
    /// <param name="spriteBatch">Спрайт-батч для отрисовки.</param>
    /// <param name="gameTime">Информация о времени игры.</param>
    public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        GameObject[] sortedObjects = _gameObjects.Values
        .Where(r => r.Transform.Visible && Camera.IsPositionVisible(r.Transform.Position))
        .OrderBy(e => -e.Transform.Position.Y + e.TextureRectangle.Height / 16)
        .ToArray();
        foreach (var gameObject in sortedObjects)
        {
            if (gameObject.CustomDraw != null)
            {
                gameObject.CustomDraw.Draw(spriteBatch, gameTime);
                continue;
            }

            spriteBatch.Draw
            (
                gameObject.Texture,
                Camera.WorldToScreen(gameObject.Transform.Position),
                gameObject.TextureRectangle,
                gameObject.Color,
                0f,
                Vector2.Zero,
                gameObject.Transform.Size,
                gameObject.TextureFlip,
                0f
            );
        }
    }

    /// <summary>
    /// Обновляет все компоненты всех объектов.
    /// </summary>
    /// <param name="gameTime">Информация о времени игры.</param>
    public static void Update(GameTime gameTime)
    {
        foreach (GameObject gameObject in _gameObjects.Values)
        {
            for (int j = 0; j < gameObject.Components.Count; j++)
            {
                gameObject.Components[j].Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Отрисовывает коллизию объекта для отладки.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch для отрисовки.</param>
    /// <param name="gameObject">Объект, коллайдер которого отображается.</param>
    public static void DrawCollider(SpriteBatch spriteBatch, GameObject gameObject)
    {
        if (Camera.IsPositionVisible(gameObject.Transform.Position))
        {
            ColliderInfo colliderInfo = gameObject.ColliderComponent.ColliderInfo;
            spriteBatch.Draw
            (
                pixel,
                Camera.WorldToScreen(gameObject.Transform.Position + new Vector2(colliderInfo.Collider.X, colliderInfo.Collider.Y)),
                new Rectangle
                (
                    0, 0, (int)colliderInfo.Collider.Width * 16, (int)colliderInfo.Collider.Height * 16
                ),
                Color.White * 0.5f
            );
        }
    }
}
