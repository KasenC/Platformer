using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platformer
{
    //Note: rotation currently not supported by this physics system. Usage on GameObjects with nonzero rotation will result in undefined behavior.

    internal class PhysicsObject: Script<GameObject>
    {
        /// <summary>
        /// Fixed objects will not move at all.
        /// Static objects will not be affected by physics effects (gravity, collisions, applied forces). However, they will move if their velocity value is assigned to.
        /// Dynamic objects' velocities will change in response to physics effects. They can only collide with fixed objects (static collision to be implemented).
        /// </summary>
        public enum Type { Fixed, Static, Dynamic }
        
        public bool feelsGravity;
        public float gravityStrength; //Will be assigned to the PhysicsManager's default value when added to the PhysicsManager.
        public readonly Type type;

        protected Vector2 velocity;

        public PhysicsObject(GameObject gameObject, Type type, bool feelsGravity = true)
        {
            gameObject.AddScript(this);
            this.type = type;
            this.feelsGravity = feelsGravity;
        }

        public override void Update(GameTime gameTime)
        {
            float timeStep = TimeStep(gameTime);
            if (type == Type.Dynamic)
            {
                if(feelsGravity)
                {
                    velocity += Vector2.UnitY * gravityStrength * timeStep;
                }
            }

            owningObject.Position += velocity * timeStep;
        }

        public Rect Bounds
        {
            get
            {
                Vector2 topLeft = owningObject.Position - owningObject.ObjectPivot;
                return new Rect(topLeft, owningObject.ObjectSize);
            }
        }

        public Vector2 Center
        {
            get => owningObject.Position - owningObject.ObjectPivot + owningObject.ObjectSize / 2f;
        }

        public void PositionSide(Side side, float value)
        {
            float delta = value - Bounds.GetSide(side);
            if (side == Side.Top || side == Side.Bottom)
                owningObject.Position.Y += delta;
            else
                owningObject.Position.X += delta;
        }

        public virtual void HandleCollision(CollisionInfo collision)
        {
            if (type != Type.Dynamic)
                return;
            PhysicsObject other = collision.obj1 == this ? collision.obj2 : collision.obj1;
            if (!Bounds.Intersects(other.Bounds))
                return;

            if(collision.side == Side.Top)
            {
                PositionSide(Side.Top, other.Bounds.Bottom + PhysicsManager.precisionOffset);
                velocity.Y = 0f;
            }
            else if(collision.side == Side.Bottom)
            {
                PositionSide(Side.Bottom, other.Bounds.Top - PhysicsManager.precisionOffset);
                velocity.Y = 0f;
            }
            else if(collision.side == Side.Left)
            {
                PositionSide(Side.Left, other.Bounds.Right + PhysicsManager.precisionOffset);
                velocity.X = 0f;
            }
            else if(collision.side == Side.Right)
            {
                PositionSide(Side.Right, other.Bounds.Left - PhysicsManager.precisionOffset);
                velocity.X = 0f;
            }
        }

        protected float TimeStep(GameTime gameTime)
        {
            return (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }

    //PhysicsManager updates after individual objects
    internal class PhysicsManager : ManagedObject
    {
        public static float precisionOffset = 0.0001f;

        /// <param name="gravityStrength"> in units/second^2</param>
        public PhysicsManager(float gravityStrength)
        {
            this.gravityStrength = gravityStrength;
        }

        public float gravityStrength;
        
        private List<PhysicsObject> staticObjects = new(), dynamicObjects = new(), fixedObjects = new();

        public void AddPhysicsObject(PhysicsObject physicsObject)
        {
            if(physicsObject.type == PhysicsObject.Type.Dynamic)
            {
                physicsObject.gravityStrength = gravityStrength;
                dynamicObjects.Add(physicsObject);
            }
            else if(physicsObject.type == PhysicsObject.Type.Static)
            {
                staticObjects.Add(physicsObject);
            }
            else if(physicsObject.type == PhysicsObject.Type.Fixed)
            {
                fixedObjects.Add(physicsObject);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected List<CollisionInfo> CheckCollision(PhysicsObject obj1, PhysicsObject obj2)
        {
            if (obj1.type != PhysicsObject.Type.Dynamic || obj2.type == PhysicsObject.Type.Dynamic)
                throw new ArgumentException("Current CheckCollision implementation: obj1 must be dynamic and obj2 must be non-dynamic");

            Rect col1 = obj1.Bounds, col2 = obj2.Bounds;
            List<Side> collidingSurfaces = new();
            Rect overlap = obj1.Bounds.Intersect(obj2.Bounds, out bool intersects);
            if (!intersects)
                return null;

            //TEMPORARY CODE - should be replaced with better surface detection.
            if(col1.Top > col2.Top) //col1 top is lower
                collidingSurfaces.Add(Side.Top);
            if(col1.Bottom < col2.Bottom) //col1 bottom is higher
                collidingSurfaces.Add(Side.Bottom);
            if(col1.Left > col2.Left) //col1 left is further right
                collidingSurfaces.Add(Side.Left);
            if(col1.Right < col2.Right) //col2 right is further left
                collidingSurfaces.Add(Side.Right);

            List<CollisionInfo> collisions = new();
            foreach(Side surface in collidingSurfaces)
            {
                float overlapDist;
                if(surface == Side.Bottom || surface == Side.Top)
                    overlapDist = overlap.Height;
                else
                    overlapDist = overlap.Width;
                collisions.Add(new(obj1, obj2, surface, overlap, overlapDist));
            }
            return collisions;
        }

        protected void CheckCollisions()
        {
            List<CollisionInfo> collisions = new();
            foreach(PhysicsObject obj1 in dynamicObjects)
            {
                foreach(PhysicsObject obj2 in staticObjects.Concat(fixedObjects))
                {
                    var objCollisions = CheckCollision(obj1, obj2);
                    if (objCollisions != null)
                        collisions.AddRange(objCollisions);
                }
            }
            foreach(CollisionInfo collision in collisions.OrderByDescending(c => c.overlapRect.Area).ThenByDescending(c => c.SurfaceLength))
            {
                PhysicsObject obj;
                if (collision.obj1.type == PhysicsObject.Type.Dynamic)
                    obj = collision.obj1;
                else if (collision.obj2.type == PhysicsObject.Type.Dynamic)
                    obj = collision.obj2;
                else
                    continue;
               obj.HandleCollision(collision);
            }
        }

        public override void Update(GameTime gameTime)
        {
            CheckCollisions();
        }
    }

    internal struct CollisionInfo
    {
        public PhysicsObject obj1, obj2;
        public Side side;
        public Rect overlapRect;
        public float overlapDistance;
        
        public bool HorizontalSurface { get => side == Side.Top || side == Side.Bottom; }

        public float SurfaceLength { get => HorizontalSurface ? overlapRect.Width : overlapRect.Height; }

        public CollisionInfo(PhysicsObject obj1, PhysicsObject obj2, Side collidedSurface, Rect overlapRect, float overlapDistance)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.side = collidedSurface;
            this.overlapRect = overlapRect;
            this.overlapDistance = overlapDistance;
        }
    }
}
