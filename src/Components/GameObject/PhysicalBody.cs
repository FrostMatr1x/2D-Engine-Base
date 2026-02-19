using System;
using System.Drawing;
using Microsoft.Xna.Framework;

namespace Engine;

public class PhysicalBody : IComponent
{
    public GameObject GameObject { get; set; }
    public Collider Collider { get; private set; }
    public Vector2 NormalForce;
    public Vector2 MoveDirection;
    public float MoveSpeed;

    public (int, int) MaxForce = new (10, 10); 

    public void PhysicalBodyInit(float moveSpeed, Collider collider)
    {
        MoveSpeed = moveSpeed;
        Collider = collider;
        Collider.ColliderEventSystem.Subscribe(CollisionUpdate, CollisionType.Block);
    }

    /// <summary>
    /// Перемещение объекта в заданном направлении
    /// x и y - направление перемещения, значения -1, 0 или 1
    /// </summary>
    /// <param name="x">x - координата</param>
    /// <param name="y">y - координата</param> 
    public void Move(int x, int y) => NormalForce += MoveSpeed * new Vector2(x, y);

    public void Stop()
    {
        if (NormalForce.X != 0)
        {
            if (NormalForce.X < 1 && NormalForce.X > -1)
            {
                NormalForce.X = 0;
            }
            else
            {
                NormalForce.X -= MathF.Sign(NormalForce.X);
            }
        }
        if (NormalForce.Y != 0)
        {
            if (NormalForce.Y < 1 && NormalForce.Y > -1)
            {
                NormalForce.Y = 0;
            }
            else
            {
                NormalForce.Y -= MathF.Sign(NormalForce.Y);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        if (NormalForce.X != 0 || NormalForce.Y != 0)
        {
            MoveDirection = new Vector2(NormalForce.X / MaxForce.Item1, -NormalForce.Y / MaxForce.Item2);
            NormalForce.X = Math.Clamp(NormalForce.X, -MaxForce.Item1, MaxForce.Item1);
            NormalForce.Y = Math.Clamp(NormalForce.Y, -MaxForce.Item2, MaxForce.Item2);
            NormalForce *= 0.95f;
            GameObject.Transform.Position += NormalForce * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        else
        {
            MoveDirection = Vector2.Zero;
        }
    }

    private void CollisionUpdate(GameObject otherObject)
    {
        if (otherObject == null) return;

        RectangleF thisCollider = GameObject.ColliderComponent.ColliderInfo.GetWorldBounds();
        RectangleF otherCollider = otherObject.ColliderComponent.ColliderInfo.GetWorldBounds();

        if (thisCollider == RectangleF.Empty || otherCollider == RectangleF.Empty) return;
        if (!GameObject.ColliderComponent.ColliderInfo.IntersectsWith(otherObject.ColliderComponent.ColliderInfo)) return;

        float overlapX;
        float overlapY;
        
        float leftOverlap = thisCollider.Right - otherCollider.Left;   // перекрытие слева
        float rightOverlap = otherCollider.Right - thisCollider.Left;  // перекрытие справа

        if (MathF.Abs(leftOverlap) < MathF.Abs(rightOverlap))
            overlapX = leftOverlap;
        else
            overlapX = -rightOverlap;

        float topOverlap = thisCollider.Bottom - otherCollider.Top;     // перекрытие снизу
        float bottomOverlap = otherCollider.Bottom - thisCollider.Top;  // перекрытие сверху

        if (MathF.Abs(topOverlap) < MathF.Abs(bottomOverlap))
            overlapY = topOverlap;
        else
            overlapY = -bottomOverlap;

        if (MathF.Abs(overlapX) < MathF.Abs(overlapY))
        {
            GameObject.Transform.Position += new Vector2(-overlapX, 0);
            NormalForce.X = 0;
        }
        else
        {
            GameObject.Transform.Position += new Vector2(0, -overlapY);
            NormalForce.Y = 0;
        }
    }
}