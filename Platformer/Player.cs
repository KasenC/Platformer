using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Platformer
{
    internal class Player:PhysicsObject
    {
        public float maxSpeed = 6f, //in units/s
            minJumpSpeed = 5f,
            maxJumpSpeed = 10f,
            maxWallSlideSpeed = 3f,
            minWallJumpSpeed = 5f,
            maxWallJumpSpeed = 10f,
            acceleration = 12f, //in units/s^2
            maxJumpCharge = .5f, //in seconds
            maxWallJumpCharge = .3f;

        protected float jumpCharge = 0f, wallJumpCharge = 0f;

        protected bool wallSliding;
        protected Side wallSlideSide;

        private bool _grounded;
        protected bool Grounded
        {
            get => _grounded;
            set
            {
                if(value)
                {
                    feelsGravity = false;
                    _grounded = true;
                }
                else
                {
                    feelsGravity = true;
                    _grounded = false;
                }
            }
        }

        public Player(GameObject obj) : base(obj, Type.Dynamic, true)
        {
            owningObject.CenterPos = CenterPos.BottomMiddle;
        }

        public override void Update(GameTime gameTime)
        {
            if (Grounded)
            {
                owningObject.ColorMask = new Color(.2f, .8f, .2f);
                wallSliding = false;
            }
            else if (wallSliding)
                owningObject.ColorMask = new Color(1f, 1f, 0f);
            else
                owningObject.ColorMask = new Color(.3f, 1f, .3f);
            HandleInput(gameTime);

            float timeStep = TimeStep(gameTime);
            if (feelsGravity)
            {
                velocity += Vector2.UnitY * gravityStrength * timeStep;
            }
            if(wallSliding && velocity.Y > maxWallSlideSpeed)
            {
                velocity.Y = maxWallSlideSpeed;
            }

            owningObject.Position += velocity * timeStep;
            Grounded = false;
            wallSliding = false;
        }

        protected void HandleInput(GameTime gameTime)
        {
            float timeStep = TimeStep(gameTime);

            if(Grounded)
            {
                int horizontalAccelDir = 0;
                if(Controls.left.GetState())
                {
                    --horizontalAccelDir;
                }
                if(Controls.right.GetState())
                {
                    ++horizontalAccelDir;
                }
                float accelAmount = acceleration * timeStep;
                if (horizontalAccelDir == 0)
                {
                    if(MathF.Abs(velocity.X) < accelAmount)
                        velocity.X = 0f;
                    else
                        horizontalAccelDir = -Math.Sign(velocity.X);
                }
                velocity.X += accelAmount * horizontalAccelDir;
                if(velocity.X > maxSpeed)
                    velocity.X = maxSpeed;
                else if(velocity.X < -maxSpeed) 
                    velocity.X = -maxSpeed;

                if(Controls.jump.GetState())
                {
                    jumpCharge += timeStep;
                }
                else
                {
                    if(jumpCharge > 0f)
                    {
                        if (jumpCharge > maxJumpCharge)
                            jumpCharge = maxJumpCharge;
                        float jumpSpeed = minJumpSpeed + (maxJumpSpeed - minJumpSpeed) * (jumpCharge / maxJumpCharge);
                        velocity.Y = -jumpSpeed;
                    }
                    jumpCharge = 0f;
                }
            }
            else
            {
                jumpCharge = 0f;
            }

            if(wallSliding)
            {
                if(Controls.jump.GetState())
                {
                    wallJumpCharge += timeStep;
                }
                else
                {
                    if(wallJumpCharge > 0f)
                    {
                        if (wallJumpCharge > maxWallJumpCharge)
                            jumpCharge = maxWallJumpCharge;
                        float wallJumpSpeed = minWallJumpSpeed + (maxWallJumpSpeed - minWallJumpSpeed) * (wallJumpCharge / maxWallJumpCharge);
                        float wallJumpDirection;
                        if (wallSlideSide == Side.Left)
                            wallJumpDirection = 1f;
                        else if (wallSlideSide == Side.Right)
                            wallJumpDirection = -1f;
                        else
                            throw new InvalidEnumArgumentException("wallSlideSide was not left or right");
                        velocity = Vector2.Normalize(new(wallJumpDirection, -1f)) * wallJumpSpeed;
                    }
                    wallJumpCharge = 0f;
                }
            }
            else
            {
                wallJumpCharge = 0f;
            }
        }

        public override void HandleCollision(CollisionInfo collision)
        {
            PhysicsObject other = collision.obj1 == this ? collision.obj2 : collision.obj1;

            Rect currentOverlap = Bounds.Intersect(other.Bounds, out bool intersects);
            if (!intersects)
                return;

            if(collision.HorizontalSurface)
            {
                if (currentOverlap.Width < 2 * PhysicsManager.precisionOffset)
                    return;

                if (collision.side == Side.Bottom)
                {
                    if (Grounded)
                        return;
                    Grounded = true;
                    PositionSide(Side.Bottom, other.Bounds.Top + PhysicsManager.precisionOffset);
                }
                else
                {
                    PositionSide(Side.Top, other.Bounds.Bottom + PhysicsManager.precisionOffset);
                }
                velocity.Y = 0f;
            }
            else
            {
                if ((currentOverlap.Height < 2 * PhysicsManager.precisionOffset)
                    || (wallSliding && wallSlideSide == collision.side))
                    return;
                int offsetSign = 1;
                if(!Grounded)
                {
                    wallSliding = true;
                    wallSlideSide = collision.side;
                    offsetSign = -1;
                }

                if (collision.side == Side.Left)
                {
                    PositionSide(Side.Left, other.Bounds.Right + PhysicsManager.precisionOffset * offsetSign);
                }
                else
                {
                    PositionSide(Side.Right, other.Bounds.Left - PhysicsManager.precisionOffset * offsetSign);
                }
                velocity.X = 0f;
            }
        }
    }
}
