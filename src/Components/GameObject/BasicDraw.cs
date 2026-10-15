using System.Numerics;
using Engine.Graphic;

namespace Engine;

/// <summary>
/// Компонент для базовой отрисовки игрового объекта с использованием текстуры и трансформации.
/// Отвечает за рендеринг объекта на экране с учётом камеры, цвета, масштаба и эффектов отражения.
/// </summary>
public class BasicDraw : IDrawableComponent, IComponent
{
    /// <summary>
    /// Ссылка на игровой объект, к которому привязан этот компонент.
    /// Используется для доступа к текстуре, позиции, размеру и другим визуальным свойствам.
    /// </summary>
    public GameObject GameObject 
    { 
        get => _gameObject;
        set
        {
            if (_gameObject == null)
            {
                _gameObject = value;
                return;
            }
            
            Debug.Error("Cannot modify GameObject variable in IComponent class."); 
        }
    }
    private GameObject _gameObject;

    /// <summary>
    /// Текстура, отображаемая для объекта.
    /// </summary>
    public Texture2D Texture
    {
        get => _gameObject.IsDestroyed ? null : _texture;
        set
        {
            if (_gameObject.IsDestroyed) { return; }
            _texture = value;
        }
    }
    private Texture2D _texture;

    /// <summary>
    /// Прямоугольник текстуры, используемый для отрисовки.
    /// </summary>
    public Rectangle TextureRectangle
    {
        get => _gameObject.IsDestroyed ? Rectangle.Empty : _rectangle;
        set
        {
            if (_gameObject.IsDestroyed) { return; }
            _rectangle = value;
        }
    }
    private Rectangle _rectangle;

    /// <summary>
    /// Флаг отражения текстуры по горизонтали.
    /// </summary>
    public SpriteEffects TextureFlip
    {
        get => _gameObject.IsDestroyed ? SpriteEffects.None : _textureFlip;
        set
        {
            if (_gameObject.IsDestroyed) { return; }
            _textureFlip = value;
        }
    }
    private SpriteEffects _textureFlip;

    /// <summary>
    /// Цвет объекта (используется при отрисовке).
    /// </summary>
    public Color Color
    {
        get => _gameObject.IsDestroyed ? Color.White : _color;
        set
        {
            if (_gameObject.IsDestroyed) { return; }
            _color = value;
        }
    }
    private Color _color;
    
    /// <summary>
    /// Выполняет отрисовку игрового объекта на экране.
    /// </summary>
    /// <param name="spriteBatch">Пакетный процесс отрисовки спрайтов, предоставляемый XNA/MonoGame.</param>
    /// <param name="gameTime">Информация о времени игры, может использоваться для анимации (не используется в данном методе).</param>
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw
        (
            Texture,
            Camera.WorldToScreen(GameObject.Transform.Position),
            TextureRectangle,
            Color,
            0f,
            Vector2.Zero,
            GameObject.Transform.Size,
            TextureFlip,
            0f
        );
    }
}