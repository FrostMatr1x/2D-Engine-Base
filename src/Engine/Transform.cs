using System.Collections.Generic;
using System.Drawing;
using Microsoft.Xna.Framework;

namespace Engine;

public interface ITransformNode
{
    Transform Transform { get; }
}

/// <summary>
/// Представляет трансформацию объекта в пространстве, включая позицию, размер и иерархию.
/// Поддерживает работу с родительскими и дочерними объектами.
/// </summary>
public class Transform
{
    /// <summary>
    /// Список дочерних трансформаций.
    /// </summary>
    public List<Transform> Childs { get; private set; } = new List<Transform>();

    /// <summary>
    /// Родительская трансформация. Если null - объект корневой.
    /// </summary>
    public Transform Parent { get; private set; }

    /// <summary>
    /// Флаг видимости объекта.
    /// Если родитель невидим, объект также считается невидимым.
    /// </summary>
    public bool Visible
    {
        get
        {
            if (Parent != null && !Parent.Visible)
            {
                return Parent.Visible;
            }
            return _visible;
        }
        set => _visible = value;
    }
    private bool _visible = true;

    /// <summary>
    /// Позиция объекта в мировых координатах.
    /// При изменении позиции автоматически обновляются позиции дочерних объектов.
    /// </summary>
    public Vector2 Position
    {
        get
        {
            return _position;
        }
        set
        {
            if (Parent != null)
            {
                Vector2 canvasDelta = value - _position;
                for (int i = 0; i < Childs.Count; i++)
                {
                    Transform child = Childs[i];
                    Vector2 newWorldPosition = child.Position + new Vector2(canvasDelta.X, canvasDelta.Y);

                    child.Position = new Vector2(newWorldPosition.X, newWorldPosition.Y);
                }
            }
            _position = value;
            CalculateSides();
        }
    }
    private Vector2 _position;

    /// <summary>
    /// Размер объекта.
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            CalculateSides();
        }
    }
    private Vector2 _size;

    /// <summary>
    /// Локальная позиция объекта относительно родителя.
    /// Если родитель отсутствует, возвращает мировую позицию.
    /// </summary>
    public Vector2 LocalPosition
    {
        get
        {
            if (Parent != null)
            {
                return Position - Parent.Position;
            }
            return Position;
        }
        set
        {
            if (Parent != null)
            {
                Position = Parent.Position + value;
                return;
            }
            Position = value;
            CalculateSides();
        }
    }

    /// <summary>
    /// Локальный размер объекта относительно родителя.
    /// </summary>
    public Vector2 LocalSize
    {
        get => Size;
        set => Size = value;
    }

    /// <summary>
    /// Координата правой границы объекта.
    /// </summary>
    public int Right;

    /// <summary>
    /// Координата левой границы объекта.
    /// </summary>
    public int Left;

    /// <summary>
    /// Координата верхней границы объекта.
    /// </summary>
    public int Top;

    /// <summary>
    /// Координата нижней границы объекта.
    /// </summary>
    public int Bottom;

    /// <summary>
    /// Локальная позиция. Хранит значение <see cref="LocalPosition"/>.
    /// </summary>
    public Vector2 _localPosition;

    /// <summary>
    /// Локальный размер. Хранит значение <see cref="LocalSize"/>.
    /// </summary>
    public Vector2 _localSize;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Transform"/>.
    /// </summary>
    /// <param name="position">Позиция объекта в мировых координатах.</param>
    /// <param name="size">Размер объекта.</param>
    public Transform(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        LocalPosition = position;
        LocalSize = size;
        CalculateSides();
    }

    /// <summary>
    /// Проверяет пересечение с другой трансформацией.
    /// </summary>
    /// <param name="value">Трансформация для проверки.</param>
    /// <returns>True, если объекты пересекаются; иначе - false.</returns>
    public bool Intersects(Transform value)
    {
        if (value.Left < Right && Left < value.Right && value.Top < Bottom)
        {
            return Top < value.Bottom;
        }
        return false;
    }

    /// <summary>
    /// Проверяет пересечение с прямоугольником XNA.
    /// </summary>
    /// <param name="value">Прямоугольник для проверки.</param>
    /// <returns>True, если объекты пересекаются; иначе - false.</returns>
    public bool Intersects(Microsoft.Xna.Framework.Rectangle value)
    {
        if (value.Left < Right && Left < value.Right && value.Top < Bottom)
        {
            return Top < value.Bottom;
        }
        return false;
    }

    /// <summary>
    /// Проверяет пересечение с прямоугольником с плавающей точкой.
    /// </summary>
    /// <param name="value">Прямоугольник для проверки.</param>
    /// <returns>True, если объекты пересекаются; иначе - false.</returns>
    public bool Intersects(RectangleF value)
    {
        if (value.Left < Right && Left < value.Right && value.Top < Bottom)
        {
            return Top < value.Bottom;
        }
        return false;
    }

    /// <summary>
    /// Пересчитывает границы объекта на основе текущей позиции и размера.
    /// </summary>
    private void CalculateSides()
    {
        Left = (int)(_position.X - _size.X / 2);
        Right = (int)(_position.X + _size.X / 2);
        Top = (int)(_position.Y - _size.Y / 2);
        Bottom = (int)(_position.Y + _size.Y / 2);
    }

    /// <summary>
    /// Добавляет дочерний узел трансформации.
    /// </summary>
    /// <param name="child">Объект, реализующий интерфейс <see cref="ITransformNode"/>.</param>
    public void AddChild(ITransformNode child)
    {
        Childs.Add(child.Transform);
        child.Transform.Parent = this;
    }

    /// <summary>
    /// Удаляет дочерний узел трансформации.
    /// </summary>
    /// <param name="child">Объект, реализующий интерфейс <see cref="ITransformNode"/>.</param>
    public void RemoveChild(ITransformNode child)
    {
        Childs.Remove(child.Transform);
        child.Transform.Parent = null;
    }
}
