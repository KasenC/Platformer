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

        public Vector2 velocity;
        public Action<PhysicsObject> DisposalFunction { private get; set; }

        public PhysicsObject(GameObject gameObject, Type type, bool feelsGravity = true):base(gameObject)
        {
            this.type = type;
            this.feelsGravity = feelsGravity;
        }

        protected override void Update(GameTime gameTime)
        {
            float timeStep = TimeStep(gameTime);
            if (type == Type.Dynamic)
            {
                if(feelsGravity)
                {
                    velocity += Vector2.UnitY * gravityStrength * timeStep;
                }
            }

            OwningObject.WorldPos += velocity * timeStep;
        }

        protected override void Destroy()
        {
            DisposalFunction?.Invoke(this);
        }

        public Rect WorldBounds
        {
            get
            {
                Rect bounds = OwningObject.WorldBounds;
                bounds.Offset(OwningObject.WorldPos);
                return bounds;
            }
        }

        public Vector2 Center
        {
            get => OwningObject.WorldPos - OwningObject.WorldPivot + OwningObject.WorldSize / 2f;
        }

        public void PositionSide(Side side, float value)
        {
            float delta = value - WorldBounds.GetSide(side);
            if (side == Side.Top || side == Side.Bottom)
                OwningObject.WorldPos += new Vector2(0f, delta);
            else
                OwningObject.WorldPos += new Vector2(delta, 0f);
        }

        public virtual void HandleCollision(CollisionInfo collision)
        {
            if (type != Type.Dynamic)
                return;
            PhysicsObject other = collision.obj1 == this ? collision.obj2 : collision.obj1;
            if (!WorldBounds.Intersects(other.WorldBounds))
                return;

            if(collision.side == Side.Top)
            {
                PositionSide(Side.Top, other.WorldBounds.Bottom + PhysicsManager.epsilon);
                velocity.Y = 0f;
            }
            else if(collision.side == Side.Bottom)
            {
                PositionSide(Side.Bottom, other.WorldBounds.Top - PhysicsManager.epsilon);
                velocity.Y = 0f;
            }
            else if(collision.side == Side.Left)
            {
                PositionSide(Side.Left, other.WorldBounds.Right + PhysicsManager.epsilon);
                velocity.X = 0f;
            }
            else if(collision.side == Side.Right)
            {
                PositionSide(Side.Right, other.WorldBounds.Left - PhysicsManager.epsilon);
                velocity.X = 0f;
            }
        }
    }


    //PhysicsManager updates after individual objects
    internal class PhysicsManager : ManagedObject
    {
        public static float epsilon = 0.0001f;

        /// <param name="gravityStrength"> in units/second^2</param>
        public PhysicsManager(IManager<ManagedObject> manager, float gravityStrength): base(manager, 10f)
        {
            this.gravityStrength = gravityStrength;
        }

        public float gravityStrength;
        
        private readonly List<PhysicsObject> staticObjects = new(), dynamicObjects = new(), fixedObjects = new();

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
            physicsObject.DisposalFunction = RemovePhysicsObject;
        }

        public void RemovePhysicsObject(PhysicsObject physicsObject)
        {
            _ = staticObjects.Remove(physicsObject) || dynamicObjects.Remove(physicsObject) || fixedObjects.Remove(physicsObject);
        }

        protected CollisionInfo? CheckCollision(PhysicsObject obj1, PhysicsObject obj2)
        {
            if (obj1.type != PhysicsObject.Type.Dynamic || obj2.type == PhysicsObject.Type.Dynamic)
                throw new ArgumentException("Current CheckCollision implementation: obj1 must be dynamic and obj2 must be non-dynamic");

            Rect col1 = obj1.WorldBounds, col2 = obj2.WorldBounds;
            Rect intersection = col1.Intersect(col2, out bool intersects);
            if (!intersects)
                return null;

            Vector2 velocity = obj1.velocity;
            int velocitySignX = MathF.Sign(velocity.X), velocitySignY = MathF.Sign(velocity.Y);
            Rect overlap;
            if(velocitySignX == 1)
            {
                overlap.Right = col1.Right;
                overlap.Left = col2.Left;
            }
            else if(velocitySignX == -1)
            {
                overlap.Left = col1.Left;
                overlap.Right = col2.Right;
            }
            else
            {
                overlap.Left = intersection.Left;
                overlap.Right = intersection.Right;
            }

            if (velocitySignY == 1)
            {
                overlap.Bottom = col1.Bottom;
                overlap.Top = col2.Top;
            }
            else if (velocitySignY == -1)
            {
                overlap.Top = col1.Top;
                overlap.Bottom = col2.Bottom;
            }
            else
            {
                overlap.Top = intersection.Top;
                overlap.Bottom = intersection.Bottom;
            }

            Vector2 overlapVector;
            Side collisionSurface;
            float surfaceLength;

            if(velocitySignX == 0 && velocitySignY == 0)
            {
                List<Side> possibleSurfaces = new();
                if (col1.Top > col2.Top) //col1 top is lower
                    possibleSurfaces.Add(Side.Top);
                if (col1.Bottom < col2.Bottom) //col1 bottom is higher
                    possibleSurfaces.Add(Side.Bottom);
                if (col1.Left > col2.Left) //col1 left is further right
                    possibleSurfaces.Add(Side.Left);
                if (col1.Right < col2.Right) //col2 right is further left
                    possibleSurfaces.Add(Side.Right);

                Side[] verticalSurfaces = new Side[] { Side.Left, Side.Right },
                    horizontalSurfaces = new Side[] { Side.Top, Side.Bottom };
                if (possibleSurfaces.Intersect(horizontalSurfaces).Count() > 1)
                    possibleSurfaces = possibleSurfaces.Except(horizontalSurfaces).ToList();
                if (possibleSurfaces.Intersect(verticalSurfaces).Count() > 1)
                    possibleSurfaces = possibleSurfaces.Except(verticalSurfaces).ToList();
                if (!possibleSurfaces.Any())
                    possibleSurfaces.Add(Side.Bottom);
                
                if(possibleSurfaces.Count > 1)
                {
                    if (overlap.Width > overlap.Height)
                        possibleSurfaces = possibleSurfaces.Union(horizontalSurfaces).ToList();
                    else
                        possibleSurfaces = possibleSurfaces.Union(verticalSurfaces).ToList();
                }
                collisionSurface = possibleSurfaces.First();

                if (collisionSurface == Side.Bottom)
                {
                    overlapVector = new(0f, overlap.Height);
                    surfaceLength = overlap.Width;
                }
                else if(collisionSurface == Side.Top)
                {
                    overlapVector = new(0f, -overlap.Height);
                    surfaceLength = overlap.Width;
                }
                else if(collisionSurface == Side.Right)
                {
                    overlapVector = new(overlap.Width, 0f);
                    surfaceLength = overlap.Height;
                }
                else
                {
                    overlapVector = new(-overlap.Width, 0f);
                    surfaceLength = overlap.Height;
                }
            }
            else
            {
                float ratio = MathF.Abs(velocity.Y / velocity.X);
                float projectedWidth = overlap.Height / ratio, projectedHeight = overlap.Width * ratio;
                Rect backtrackedCol1 = new(col1);
                if (projectedWidth > overlap.Width)
                {
                    collisionSurface = velocitySignX == 1 ? Side.Right : Side.Left;
                    overlapVector = new Vector2(overlap.Width * velocitySignX, projectedHeight * velocitySignY);
                    backtrackedCol1.Offset(-overlapVector);
                    Rect initialIntersect = backtrackedCol1.Intersect(col2);
                    surfaceLength = initialIntersect.Height;
                }
                else
                {
                    collisionSurface = velocitySignY == 1 ? Side.Bottom : Side.Top;
                    overlapVector = new Vector2(projectedWidth * velocitySignX, overlap.Height * velocitySignY);
                    backtrackedCol1.Offset(-overlapVector);
                    Rect initialIntersect = backtrackedCol1.Intersect(col2);
                    surfaceLength = initialIntersect.Width;
                }
            }

            return new(obj1, obj2, collisionSurface, intersection, overlapVector, surfaceLength);
        }

        protected void CheckCollisions()
        {
            List<CollisionInfo> collisions = new();
            foreach(var obj1 in dynamicObjects)
            {
                foreach(var obj2 in staticObjects.Concat(fixedObjects))
                {
                    var objCollisions = CheckCollision(obj1, obj2);
                    if (objCollisions.HasValue)
                        collisions.Add(objCollisions.Value);
                }
            }
            foreach (CollisionInfo collision in collisions.OrderByDescending(c => c.overlapVector.LengthSquared()).ThenByDescending(c => c.surfaceLength))
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

        protected override void Update(GameTime gameTime)
        {
            CheckCollisions();
        }
    }

    internal struct CollisionInfo
    {
        public PhysicsObject obj1, obj2;
        public Side side;
        public Rect intersection;
        public Vector2 overlapVector;
        public float surfaceLength;

        public bool HorizontalSurface => side == Side.Top || side == Side.Bottom;

        public float OverlapNormalComponent => HorizontalSurface ? overlapVector.Y : overlapVector.X;

        public float OverlapParallelComponent => HorizontalSurface ? overlapVector.X : overlapVector.Y;

        public CollisionInfo(PhysicsObject obj1, PhysicsObject obj2, Side collidedSurface, Rect intersection, Vector2 overlapVector, float surfaceLength)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.side = collidedSurface;
            this.intersection = intersection;
            this.overlapVector = overlapVector;
            this.surfaceLength = surfaceLength;
        }
    }
}
