using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platformer
{
    //Note: rotation currently not supported by this physics system. Usage on GameObjects with nonzero rotation will result in undefined behavior.

    internal class PhysicsObject: Script
    {
        /// <summary>
        /// Fixed objects will not move at all.
        /// Static objects will not be affected by physics effects (gravity, collisions, applied forces). However, they will move if their velocity value is assigned to.
        /// Dynamic objects' velocities will change in response to physics effects. They can only collide with fixed objects (static collision to be implemented).
        /// </summary>
        public enum Type { Fixed, Static, Dynamic }

        public static float precisionOffset = 0.0001f;
        
        public bool feelsGravity;
        public float gravityStrength; //Will be assigned to the PhysicsManager's default value when added to the PhysicsManager.
        public Type type;

        Vector2 velocity;

        public PhysicsObject(GameObject gameObject, Type type, bool feelsGravity = true)
        {
            gameObject.AddScript(this);
            this.type = type;
            this.feelsGravity = feelsGravity;
        }

        public override void Update(GameTime gameTime)
        {
            float timeStep = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (type == Type.Dynamic)
            {
                if(feelsGravity)
                {
                    velocity += Vector2.UnitY * gravityStrength * timeStep;
                }
            }

            gameObject.Position += velocity * timeStep;
        }

        public Rect collisionBox
        {
            get
            {
                Vector2 topLeft = gameObject.Position - gameObject.ObjectCenter;
                return new Rect(topLeft, gameObject.Size);
            }
        }

        public virtual void HandleCollision(CollisionInfo collision)
        {
            if (type != Type.Dynamic)
                return;
            if(collision.surface == CollisionInfo.Surface.Top)
            {
                gameObject.Position.Y += collision.overlapDistance + precisionOffset;
                velocity.Y = 0f;
            }
            else if(collision.surface == CollisionInfo.Surface.Bottom)
            {
                gameObject.Position.Y -= collision.overlapDistance + precisionOffset;
                velocity.Y = 0f;
            }
            else if(collision.surface == CollisionInfo.Surface.Left)
            {
                gameObject.Position.X += collision.overlapDistance + precisionOffset;
                velocity.X = 0f;
            }
            else if(collision.surface == CollisionInfo.Surface.Right)
            {
                gameObject.Position.X -= collision.overlapDistance + precisionOffset;
                velocity.X = 0f;
            }
        }
    }

    //PhysicsManager updates after individual objects
    internal class PhysicsManager : IManaged
    {
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

            Rect col1 = obj1.collisionBox, col2 = obj2.collisionBox;
            List<CollisionInfo.Surface> collidingSurfaces = new();
            Rect overlap = obj1.collisionBox.Intersect(obj2.collisionBox);
            if (overlap.Height <= 0f || overlap.Width <= 0f)
                return null;

            //TEMPORARY CODE - should be replaced with better surface detection.
            if(col1.Top > col2.Top) //col1 top is lower
                collidingSurfaces.Add(CollisionInfo.Surface.Top);
            if(col1.Bottom < col2.Bottom) //col1 bottom is higher
                collidingSurfaces.Add(CollisionInfo.Surface.Bottom);
            if(col1.Left > col2.Left) //col1 left is further right
                collidingSurfaces.Add(CollisionInfo.Surface.Left);
            if(col1.Right < col2.Right) //col2 right is further left
                collidingSurfaces.Add(CollisionInfo.Surface.Right);

            List<CollisionInfo> collisions = new();
            foreach(CollisionInfo.Surface surface in collidingSurfaces)
            {
                float overlapDist;
                if(surface == CollisionInfo.Surface.Bottom || surface == CollisionInfo.Surface.Top)
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
                if (!collision.obj1.collisionBox.Intersects(collision.obj2.collisionBox))
                    continue;
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

        public void Initialize()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            CheckCollisions();
        }

        public void DrawUpdate(GameTime gameTime)
        {
            
        }
    }

    internal struct CollisionInfo
    {
        public enum Surface { Top, Right, Bottom, Left }

        public PhysicsObject obj1, obj2;
        public Surface surface;
        public Rect overlapRect;
        public float overlapDistance;
        
        public bool HorizontalSurface { get => surface == Surface.Top || surface == Surface.Bottom; }

        public float SurfaceLength { get => HorizontalSurface ? overlapRect.Width : overlapRect.Height; }

        public CollisionInfo(PhysicsObject obj1, PhysicsObject obj2, Surface collidedSurface, Rect overlapRect, float overlapDistance)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.surface = collidedSurface;
            this.overlapRect = overlapRect;
            this.overlapDistance = overlapDistance;
        }

    }

    internal struct Rect
    {
        //public float Width, Height, X, Y;

        public float Top = 0f, Left = 0f, Bottom = 0f, Right = 0f;

        public float X
        {
            get => Left;
            set
            {
                float w = Width;
                Left = value;
                Right = value + w;
            }
        }

        public float Y
        {
            get => Top;
            set
            {
                float h = Height;
                Top = value;
                Bottom = value + h;
            }
        }

        public float Width
        {
            get => Right - Left;
            set => Right = Left + value;
        }

        public float Height
        {
            get => Bottom - Top;
            set => Bottom = Top + value;
        }

        public Vector2 Location 
        { 
            get => new Vector2(X, Y); 
            set 
            { 
                X = value.X;
                Y = value.Y;
            }
        }

        public Vector2 Size 
        { 
            get => new Vector2(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        public float Area
        {
            get => Width * Height;
        }

        public Rect(Vector2 location, Vector2 size)
        {
            Location = location;
            Size = size;
        }

        public Rect(float X, float Y, float Width, float Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public Rect Intersect(Rect rect)
        {
            Rect intersect = new();
            intersect.Top = MathF.Max(Top, rect.Top);
            intersect.Bottom = MathF.Min(Bottom, rect.Bottom);
            intersect.Left = MathF.Max(Left, rect.Left);
            intersect.Right = MathF.Min(Right, rect.Right);
            return intersect;
        }

        public bool Intersects(Rect rect)
        {
            Rect intersect = Intersect(rect);
            return intersect.Height > 0f && intersect.Width > 0f;
        }
    }
}
