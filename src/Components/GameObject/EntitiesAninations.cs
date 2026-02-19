using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Sprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blockhand;
public abstract class AnimationTriggerBase
{
    public abstract Type ValueType { get; }
    public abstract object Value { get; }
    public abstract object TriggerValue { get; set; }
    public abstract Animation Animation { get; set; }

    public abstract bool ValidateValue();
}

public class AnimationTrigger<T> : AnimationTriggerBase
{
    public override Animation Animation { get; set; }
    private readonly Func<T> _getter;
    public override object TriggerValue { get; set; }
    public override object Value => _getter();
    public override Type ValueType => typeof(T);

    public AnimationTrigger(Func<T> getter, T triggerValue, Animation animation)
    {
        _getter = getter;
        TriggerValue = triggerValue;
        Animation = animation;
    }

    public override bool ValidateValue() => EqualityComparer<T>.Default.Equals(_getter(), (T)TriggerValue);
}

public class Animation
{
    public Func<Vector2> Position;
    public SpriteEffects SpriteEffect;
    public bool SaveFirstFrame;
    public AnimationSprite AnimationSprite { get; private set; }
    
    public int AnimationId { get; private set; }

    public Animation(Func<Vector2> position, AnimationSprite animationSprite, int animationId, SpriteEffects? spriteEffect, bool saveFirstFrame = false)
    {
        Position = position;
        AnimationId = animationId;
        AnimationSprite = animationSprite;
        SpriteEffect = spriteEffect ?? SpriteEffects.None;
        SaveFirstFrame = saveFirstFrame;
    }

    public void PlayAnimation(GameTime gameTime) 
    {
        AnimationSprite.PaintAnimation(
         gameTime,
         Camera.WorldToScreen(Position()),
         new Vector2(1, 1),
         AnimationId,
         SpriteEffect
         );
    }
}

public class AnimationGroup
{
    public Animation[] Animations;

    public AnimationGroup(Animation[] animations)
    {
        Animations = animations;
    }
}

/// <summary>
/// Группа триггеров анимаций, позволяющая устанавливать приоритеты воспроизведения.
/// При проверке состояния сначала оцениваются приоритетные анимации, затем - остальные.
/// </summary>
public class AnimationTriggerGroup
{
    /// <summary>
    /// Список анимаций без приоритета, которые проверяются, только если ни одна приоритетная не активна.
    /// </summary>
    public List<AnimationTriggerBase> Animations;

    /// <summary>
    /// Список анимаций с приоритетом. Проверяются первыми при обновлении состояния группы.
    /// </summary>
    public List<AnimationTriggerBase> AnimationPriority;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AnimationTriggerGroup"/> с указанным массивом триггеров анимаций.
    /// Все триггеры изначально добавляются в список <see cref="Animations"/>.
    /// </summary>
    /// <param name="animations">Массив триггеров анимаций, входящих в группу.</param>
    public AnimationTriggerGroup(AnimationTriggerBase[] animations)
    {
        Animations = animations.ToList();
        AnimationPriority = new List<AnimationTriggerBase>();
    }

    /// <summary>
    /// Устанавливает приоритет для анимации по её индексу в списке <see cref="Animations"/>.
    /// Если индекс некорректен или анимация уже в приоритете, операция игнорируется.
    /// </summary>
    /// <param name="id">Индекс анимации в списке <see cref="Animations"/>.</param>
    public void SetPriority(int id)
    {
        if (id < 0 || id >= Animations.Count || AnimationPriority.Contains(Animations[id])) return;
        AnimationPriority.Add(Animations[id]);
        Animations.Remove(Animations[id]);
    }

    /// <summary>
    /// Устанавливает приоритет для указанного триггера анимации.
    /// Если триггер равен null или уже находится в приоритетном списке, операция игнорируется.
    /// </summary>
    /// <param name="animationTriggerBase">Триггер анимации, которому нужно установить приоритет.</param>
    public void SetPriority(AnimationTriggerBase animationTriggerBase)
    {
        if (animationTriggerBase == null || AnimationPriority.Contains(animationTriggerBase)) return;
        AnimationPriority.Add(animationTriggerBase);
        Animations.Remove(animationTriggerBase);
    }

    /// <summary>
    /// Убирает приоритет у анимации по индексу в списке <see cref="AnimationPriority"/> и возвращает её в основной список.
    /// Если индекс некорректен, операция игнорируется.
    /// </summary>
    /// <param name="id">Индекс анимации в списке <see cref="AnimationPriority"/>.</param>
    public void RemovePriority(int id)
    {
        if (id < 0 || id >= AnimationPriority.Count) return;
        var trigger = AnimationPriority[id];
        AnimationPriority.RemoveAt(id);
        Animations.Add(trigger);
    }

    /// <summary>
    /// Убирает приоритет у указанного триггера анимации и возвращает его в основной список.
    /// Если триггер равен null или не находится в приоритетном списке, операция игнорируется.
    /// </summary>
    /// <param name="animationTriggerBase">Триггер анимации, у которого нужно убрать приоритет.</param>
    public void RemovePriority(AnimationTriggerBase animationTriggerBase)
    {
        if (animationTriggerBase == null || !AnimationPriority.Contains(animationTriggerBase)) return;
        AnimationPriority.Remove(animationTriggerBase);
        Animations.Add(animationTriggerBase);
    }
}


/// <summary>
/// Компонент, отвечающий за управление анимациями игрового объекта. Поддерживает одиночные анимации, триггеры на основе значений и группы анимаций с приоритетами.
/// </summary>
public class EntitiesAnimations : IComponent, IDrawableComponent
{
    /// <summary>
    /// Список анимаций, привязанных к условным триггерам (например, при изменении значения).
    /// </summary>
    public List<AnimationTriggerBase> _animationWithTrigger { get; private set; } = new List<AnimationTriggerBase>();

    /// <summary>
    /// Список всех созданных анимаций, не привязанных к триггерам напрямую.
    /// </summary>
    public List<Animation> _animations { get; private set; } = new List<Animation>();

    /// <summary>
    /// Список групп анимаций с триггерами, поддерживающих приоритетное воспроизведение.
    /// </summary>
    public List<AnimationTriggerGroup> _animationTriggerGroups { get; private set; } = new List<AnimationTriggerGroup>();

    /// <summary>
    /// Список групп анимаций без триггеров, используемых для логической группировки.
    /// </summary>
    public List<AnimationGroup> _animationGroups { get; private set; } = new List<AnimationGroup>();

    /// <summary>
    /// Ссылка на игровой объект, к которому привязан этот компонент.
    /// </summary>
    public GameObject GameObject { get; set; }

    /// <summary>
    /// Последняя активная анимация. Используется для сохранения первого кадра после окончания воспроизведения.
    /// </summary>
    private Animation _lastAnimation = null;

    /// <summary>
    /// Отрисовывает текущую активную анимацию или статичный спрайт объекта.
    /// Проверяет триггеры в порядке приоритета: сначала группы с приоритетом, затем обычные группы, затем одиночные триггеры.
    /// </summary>
    /// <param name="spriteBatch">Спрайт-батч для отрисовки.</param>
    /// <param name="gameTime">Информация о времени игры.</param>
    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Animation activeAnimation = null;

        // Сначала проверяем приоритетные группы
        foreach (var group in _animationTriggerGroups)
        {
            foreach (var trigger in group.AnimationPriority)
            {
                if (trigger.ValidateValue())
                {
                    activeAnimation = trigger.Animation;
                    break;
                }
            }
            if (activeAnimation != null) break;

            foreach (var trigger in group.Animations)
            {
                if (trigger.ValidateValue())
                {
                    activeAnimation = trigger.Animation;
                    break;
                }
            }
            if (activeAnimation != null) break;
        }

        // Потом одиночные триггеры (если групп нет или ни одна не активна)
        if (activeAnimation == null)
        {
            foreach (var trigger in _animationWithTrigger)
            {
                if (trigger.ValidateValue())
                {
                    activeAnimation = trigger.Animation;
                    break;
                }
            }
        }

        if (activeAnimation != null)
        {
            activeAnimation.PlayAnimation(gameTime);
            _lastAnimation = activeAnimation;
        }
        else if (GameObject != null)
        {
            if (_lastAnimation != null && _lastAnimation.SaveFirstFrame)
            {
                GameObject.Texture = _lastAnimation.AnimationSprite.SpriteList;
                GameObject.TextureRectangle = _lastAnimation.AnimationSprite.Sprites[_lastAnimation.AnimationId, 0];
                GameObject.TextureFlip = _lastAnimation.SpriteEffect;
                _lastAnimation = null;
            }

            // Рисуем статичный спрайт
            if (GameObject.Texture != null)
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
    }

    /// <summary>
    /// Создаёт анимацию, запускаемую при совпадении значения из <paramref name="getter"/> с <paramref name="triggerValue"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения, по которому проверяется условие запуска анимации.</typeparam>
    /// <param name="position">Функция, возвращающая позицию объекта для отрисовки.</param>
    /// <param name="animationSprite">Анимационный спрайт.</param>
    /// <param name="animationId">Идентификатор анимации (например, строка в матрице спрайтов).</param>
    /// <param name="spriteEffects">Эффекты отрисовки (отражение по горизонтали/вертикали).</param>
    /// <param name="getter">Функция, возвращающая текущее значение для проверки (например, () => player.Health).</param>
    /// <param name="triggerValue">Значение, при котором будет запущена анимация.</param>
    /// <returns>Созданный триггер анимации.</returns>
    public AnimationTrigger<T> CreateAutoAnimationByValue<T>(Func<Vector2> position, AnimationSprite animationSprite, int animationId, SpriteEffects spriteEffects, Func<T> getter, T triggerValue)
    {
        Animation animation = CreateAnimation(position, animationSprite, animationId, spriteEffects);
        AnimationTrigger<T> animationTrigger = new AnimationTrigger<T>(getter, triggerValue, animation);
        _animationWithTrigger.Add(animationTrigger);

        return animationTrigger;
    }

    /// <summary>
    /// Создаёт анимацию без привязки к триггеру.
    /// </summary>
    /// <param name="position">Функция, возвращающая позицию объекта.</param>
    /// <param name="animationSprite">Анимационный спрайт.</param>
    /// <param name="animationId">Идентификатор анимации.</param>
    /// <param name="spriteEffects">Эффекты отрисовки.</param>
    /// <returns>Созданная анимация.</returns>
    public Animation CreateAnimation(Func<Vector2> position, AnimationSprite animationSprite, int animationId, SpriteEffects spriteEffects)
    {
        Animation animation = new Animation(position, animationSprite, animationId, spriteEffects);
        _animations.Add(animation);
        return animation;
    }

    /// <summary>
    /// Создаёт группу анимаций без триггеров. Используется для логической группировки.
    /// </summary>
    /// <param name="animations">Массив анимаций, входящих в группу.</param>
    /// <returns>Созданная группа анимаций.</returns>
    public AnimationGroup CreateAnimationGroup(Animation[] animations)
    {   
        AnimationGroup animationGroup = new AnimationGroup(animations);
        _animationGroups.Add(animationGroup);
        return animationGroup;
    }

    /// <summary>
    /// Создаёт группу анимаций с триггерами, поддерживающую приоритетную проверку.
    /// </summary>
    /// <param name="animations">Массив триггеров анимаций.</param>
    /// <returns>Созданная группа с триггерами.</returns>
    public AnimationTriggerGroup CreateAnimationTriggerGroup(AnimationTriggerBase[] animations)
    {
        AnimationTriggerGroup animationTriggerGroup = new AnimationTriggerGroup(animations);
        _animationTriggerGroups.Add(animationTriggerGroup);
        return animationTriggerGroup;
    }

    /// <summary>
    /// Удаляет указанную группу анимаций из списка.
    /// </summary>
    /// <param name="animationGroup">Группа анимаций для удаления.</param>
    public void DestroyAnimationGroup(AnimationGroup animationGroup) => _animationGroups.Remove(animationGroup);

    /// <summary>
    /// Удаляет указанную группу триггеров анимаций из списка.
    /// </summary>
    /// <param name="animationTriggerGroup">Группа триггеров для удаления.</param>
    public void DestroyAnimationGroup(AnimationTriggerGroup animationTriggerGroup) => _animationTriggerGroups.Remove(animationTriggerGroup);

    /// <summary>
    /// Удаляет указанную анимацию из списка.
    /// </summary>
    /// <param name="animation">Анимация для удаления.</param>
    public void DestroyAnimation(Animation animation) => _animations.Remove(animation);

    /// <summary>
    /// Удаляет триггер анимации и связанную с ним анимацию.
    /// </summary>
    /// <param name="animationTriggerDataBase">Триггер анимации для удаления.</param>
    public void DestroyAnimation(AnimationTriggerBase animationTriggerDataBase)
    {
        _animations.Remove(animationTriggerDataBase.Animation);
        _animationWithTrigger.Remove(animationTriggerDataBase);
    }
}
