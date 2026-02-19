using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using _2D_Engine_Base;
using Blockhand;
using Microsoft.Xna.Framework;

namespace Engine;

/// <summary>
/// Определяет типы столкновений, которые могут быть обработаны системой.
/// </summary>
public enum CollisionType { All, Block, Entity }

/// <summary>
/// Система событий столкновений, позволяющая подписываться на события определённого типа и получать уведомления при их возникновении.
/// Поддерживает разные типы столкновений: со всеми объектами, только с блоками или только с сущностями.
/// </summary>
public class ColliderEventSystem
{
    /// <summary>
    /// Хранилище подписок на события столкновений, где ключ — тип столкновения, а значение — цепочка делегатов.
    /// </summary>
    private Dictionary<CollisionType, Action<GameObject>> _subscriptions = new Dictionary<CollisionType, Action<GameObject>>();

    /// <summary>
    /// Подписывает указанный метод на событие столкновения заданного типа.
    /// Если для этого типа ещё нет подписок, создаётся новая запись.
    /// </summary>
    /// <param name="action">Метод, который будет вызван при столкновении.</param>
    /// <param name="type">Тип столкновения, на который осуществляется подписка.</param>
    public void Subscribe(Action<GameObject> action, CollisionType type)
    {
        _subscriptions.TryAdd(type, null);
        _subscriptions[type] = _subscriptions[type] + action;
    }

    /// <summary>
    /// Отписывает указанный метод от события столкновения заданного типа.
    /// Если тип не существует или метод не был подписан, операция игнорируется.
    /// </summary>
    /// <param name="action">Метод, который нужно отписать.</param>
    /// <param name="type">Тип столкновения, от которого осуществляется отписка.</param>
    public void Unsubscribe(Action<GameObject> action, CollisionType type)
    {
        if (!_subscriptions.ContainsKey(type)) return;
        _subscriptions[type] = _subscriptions[type] - action;
    }

    /// <summary>
    /// Вызывает все подписанные методы для указанного типа столкновения.
    /// Передаёт информацию о втором объекте в столкновении.
    /// </summary>
    /// <param name="type">Тип столкновения, которое произошло.</param>
    /// <param name="other">Объект, с которым произошло столкновение.</param>
    public void Invoke(CollisionType type, GameObject other)
    {
        if (_subscriptions.TryGetValue(type, out var action) && action != null)
        {
            action(other);
        }
    }
}

/// <summary>
/// Информация о коллайдере, включающая его размеры и положение относительно трансформации объекта.
/// Обеспечивает корректный расчёт мировых координат и проверку пересечений.
/// </summary>
public class ColliderInfo
{
    /// <summary>
    /// Локальные границы коллайдера (относительно позиции объекта).
    /// При изменении автоматически обновляются мировые координаты при следующем обращении.
    /// </summary>
    public RectangleF Collider
    {
        get => collider;
        set => collider = value;
    }
    private RectangleF collider;

    /// <summary>
    /// Ссылка на трансформацию, к которой привязан этот коллайдер.
    /// Используется для вычисления мировых координат.
    /// </summary>
    private Transform transform;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ColliderInfo"/> с привязкой к указанной трансформации.
    /// </summary>
    /// <param name="transform">Трансформация объекта, к которому привязан коллайдер.</param>
    public ColliderInfo(Transform transform) => this.transform = transform;

    /// <summary>
    /// Проверяет, пересекается ли данный коллайдер с другим.
    /// Использует мировые координаты для точного определения пересечения.
    /// </summary>
    /// <param name="colliderInfo">Другой коллайдер для проверки пересечения.</param>
    /// <returns>Значение true, если коллайдеры пересекаются; иначе — false.</returns>
    public bool IntersectsWith(ColliderInfo colliderInfo)
    {
        return GetWorldBounds().IntersectsWith(colliderInfo.GetWorldBounds());
    }

    /// <summary>
    /// Возвращает границы коллайдера в мировых координатах с учётом позиции объекта.
    /// Учитывает смещение по оси Y для корректного отображения в 2D-пространстве.
    /// </summary>
    /// <returns>Прямоугольник в мировых координатах, представляющий область коллайдера.</returns>
    public RectangleF GetWorldBounds() => 
        new RectangleF(
            Collider.X + transform.Position.X,
            Collider.Y + transform.Position.Y - Collider.Height,
            Collider.Width,
            Collider.Height
        );
}

/// <summary>
/// Определяет слои столкновений, в которых может участвовать объект.
/// Позволяет фильтровать типы объектов, с которыми возможны столкновения.
/// </summary>
public enum CollisionLayer { Null, Blocks, Entities }

/// <summary>
/// Компонент коллайдера, отвечающий за обнаружение столкновений с другими объектами в игре.
/// Поддерживает подписку на события столкновений и работает с различными слоями физики.
/// </summary>
public class Collider : IComponent
{
    /// <summary>
    /// Система событий, управляющая вызовом обработчиков при столкновениях.
    /// Позволяет реагировать на столкновения с разными типами объектов.
    /// </summary>
    public ColliderEventSystem ColliderEventSystem { get; private set; } = new ColliderEventSystem();

    /// <summary>
    /// Набор слоёв столкновений, в которых участвует данный объект.
    /// Определяет, с какими категориями объектов будет проверяться пересечение.
    /// </summary>
    public HashSet<CollisionLayer> CollisionLayers { get; set; } = new HashSet<CollisionLayer>();

    /// <summary>
    /// Ссылка на игровой объект, к которому привязан этот компонент.
    /// Используется для доступа к трансформации и другим компонентам.
    /// </summary>
    public GameObject GameObject { get; set; }

    /// <summary>
    /// Информация о границах и положении коллайдера.
    /// Включает локальные размеры и методы для проверки пересечений в мировом пространстве.
    /// </summary>
    public ColliderInfo ColliderInfo
    {
        get => _colliderInfo;
        set => _colliderInfo = value;
    }
    private ColliderInfo _colliderInfo;

    /// <summary>
    /// Инициализирует коллайдер с указанным размером и массивом слоёв столкновений.
    /// Привязывает коллайдер к трансформации объекта и настраивает проверку столкновений.
    /// </summary>
    /// <param name="ColliderSize">Размер коллайдера в единицах мира.</param>
    /// <param name="layers">Массив слоёв, с которыми будет проверяться столкновение.</param>
    public void InitCollider(Vector2 ColliderSize, CollisionLayer[] layers)
    {
        ColliderInfo = new ColliderInfo(GameObject.Transform);
        ColliderInfo.Collider = new RectangleF(0, 0, ColliderSize.X, ColliderSize.Y);
        if (layers != null) 
        {
            CollisionLayers = layers.ToHashSet();
        }
    }

    /// <summary>
    /// Инициализирует коллайдер с указанным размером и одним слоем столкновений.
    /// Упрощённая версия метода для случаев, когда достаточно одного слоя.
    /// </summary>
    /// <param name="ColliderSize">Размер коллайдера в единицах мира.</param>
    /// <param name="layer">Слой столкновения, с которым будет проверяться пересечение.</param>
    public void InitCollider(Vector2 ColliderSize, CollisionLayer layer)
    {
        ColliderInfo = new ColliderInfo(GameObject.Transform);
        ColliderInfo.Collider = new RectangleF(0, 0, ColliderSize.X, ColliderSize.Y);
        CollisionLayer[] collisionLayers = { layer };
        CollisionLayers = collisionLayers.ToHashSet();
    }

    /// <summary>
    /// Обновляет состояние коллайдера каждый кадр.
    /// Проверяет столкновения с объектами из соответствующих слоёв.
    /// Вызывается движком игры в рамках цикла обновления.
    /// </summary>
    /// <param name="gameTime">Информация о времени игры, передаваемая движком.</param>
    public void Update(GameTime gameTime) => UpdateCollision();

    /// <summary>
    /// Выполняет проверку столкновений с объектами в зависимости от настроенных слоёв.
    /// Проверяет пересечения с блоками (через чанки) и сущностями (NPC).
    /// При обнаружении столкновения вызывает соответствующие события.
    /// </summary>
    private void UpdateCollision()
    {
        // Добовление столкновений работает подобным образом

        // if (CollisionLayers.Contains(CollisionLayer.Blocks))
        // {
        //     Game1.Instance.ChunksSystem.GetNearChunks(GameObject.Transform.Position, out List<GameObject> chunkX, out List<GameObject> chunkY);
            
        //     List<GameObject> gameObjects = Game1.Instance.ChunksSystem.GetChunk(ChunksController.ChunkId(GameObject.Transform.Position));
        //     gameObjects.AddRange(chunkX);
        //     gameObjects.AddRange(chunkY);

        //     foreach (GameObject gameObject in gameObjects)
        //     {
        //         if (gameObject.ColliderComponent.ColliderInfo.IntersectsWith(ColliderInfo))
        //         {
        //             ColliderEventSystem.Invoke(CollisionType.Block, gameObject);
        //             ColliderEventSystem.Invoke(CollisionType.All, gameObject);
        //         }
        //     }
        // }
    }
}