using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Engine.Graphic;

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
    public Rectangle TextureRectangle { get; set; }
}

/// <summary>
/// Представляет базовый игровой объект, содержащий Transform, текстуру, компоненты и систему управления жизненным циклом.
/// </summary>
public class GameObject : ITransformNode
{
    private static Dictionary<int, GameObject> _gameObjects = new Dictionary<int, GameObject>();
    private static Stack<int> _freeIds = new Stack<int>();
    private static int _nextId = 0;
    private static bool _IsInit = false;
    private int _objectId;

    public event Action OnDestroyed;

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
    /// Имя объекта.
    /// </summary>
    public string Name
    {
        get => IsDestroyed ? "" : _name;
        set
        {
            if (IsDestroyed) { return; }
            _name = value;
        }
    }
    private string _name;

    /// <summary>
    /// Компонент собственной отрисовки, связанный с объектом.
    /// </summary>
    public IDrawableComponent DrawComponent
    {
        get => IsDestroyed ? null : _drawComponent;
        set
        {
            if (IsDestroyed) { return; }
            _drawComponent = value;
        }
    }
    private IDrawableComponent _drawComponent;
    private bool _isBasicDraw = true;

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
    public GameObject(string name, Vector2 position, Texture2D spriteList, Rectangle textuerRectangle, Color? color, SpriteEffects textureFlip = SpriteEffects.None, bool visible = true)
    {
        if (spriteList != null)
        {
            _objectId = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
            Name = name;
            Transform = new Transform(position, new Vector2(1, 1))
            {
                Visible = visible
            };

            _gameObjects.Add(_objectId, this);
            
            BasicDraw basicDraw = AddComponent<BasicDraw>();

            basicDraw.TextureRectangle = textuerRectangle;
            basicDraw.Color = color ?? Color.White;
            basicDraw.Texture = spriteList;
            basicDraw.TextureFlip = textureFlip;
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
    public GameObject(string name, Vector2 position, Texture2D displayTexture, Color? color, SpriteEffects textureFlip = SpriteEffects.None, bool visible = true)
    {
        if (displayTexture != null)
        {
            _objectId = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
            Name = name;
            Transform = new Transform(position, new Vector2(1, 1))
            {
                Visible = visible
            };

            _gameObjects.Add(_objectId, this);

            BasicDraw basicDraw = AddComponent<BasicDraw>();

            basicDraw.Texture = displayTexture;
            basicDraw.TextureRectangle = new Rectangle(0, 0, displayTexture.Width, displayTexture.Height);
            basicDraw.Color = color ?? Color.White;
            basicDraw.TextureFlip = textureFlip;
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
        T component = new T
        {
            GameObject = this
        };
        Components.Add(component);
        component.Init();

        if (component is Collider collider)
        {
            _cachedCollider = collider;
        }

        if (component is IDrawableComponent drawableComponent)
        {
            if (DrawComponent == null || _isBasicDraw)
            {
                DrawComponent = drawableComponent;
                _isBasicDraw = (component is BasicDraw) ? false : true;
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

    public List<IComponent> GetAllComponents() => new List<IComponent>(Components);

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
        OnDestroyed?.Invoke();
    }

    /// <summary>
    /// Инициализирует статические ресурсы.
    /// </summary>
    /// <param name="graphicsDevice">Графическое устройство.</param>
    public static void Init(GraphicsDevice graphicsDevice)
    {
        if (_IsInit) return;
        
        pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        _IsInit = true;
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
        .OrderBy(e => -e.Transform.Position.Y + e.DrawComponent.TextureRectangle.Height / 16)
        .ToArray();

        foreach (var gameObject in sortedObjects)
        {
            if (gameObject.DrawComponent != null)
            {
                gameObject.DrawComponent.Draw(spriteBatch, gameTime);
                continue;
            }
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
