using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    public GameObject GameObject { get; set; }
    
    /// <summary>
    /// Выполняет отрисовку игрового объекта на экране.
    /// </summary>
    /// <param name="spriteBatch">Пакетный процесс отрисовки спрайтов, предоставляемый XNA/MonoGame.</param>
    /// <param name="gameTime">Информация о времени игры, может использоваться для анимации (не используется в данном методе).</param>
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw
        (
            GameObject.Texture,
            Camera.WorldToScreen(GameObject.Transform.Position),
            GameObject.TextureRectangle,
            GameObject.Color,
            0f,
            Vector2.Zero,
            GameObject.Transform.Size,
            GameObject.TextureFlip,
            0f
        );
    }
}