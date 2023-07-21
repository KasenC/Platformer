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
            minJumpSpeed = 4f,
            maxJumpSpeed = 10f,
            maxWallSlideSpeed = 3f,
            minWallJumpSpeed = 5f,
            maxWallJumpSpeed = 10f,
            ledgeClimbSpeed = 2f,
            acceleration = 12f, //in units/s^2
            wallSlideDrag = .5f,
            maxJumpCharge = .3f, //in seconds
            maxWallJumpCharge = .3f,
            wallJumpRatio = 1.5f, //Ratio of vertical velocity to horizontal velocity when walljumping
            ledgeClimbRatio = 0.95f; //Height (relative to character) of ledge which can be climbed

        public bool enableGodMode = false;

        protected float jumpCharge = 0f, wallJumpCharge = 0f;

        protected bool godMode = false;

        protected bool Grounded;
        protected bool wallSliding, ledgeClimbing;
        protected Side wallSlideSide;
        protected float ledgeHeight;

        public Player(GameObject obj) : base(obj, Type.Dynamic, true)
        {
            OwningObject.CenterPos = CenterPos.BottomMiddle;
        }

        public override void Update(GameTime gameTime)
        {
            HandleInput(gameTime);
            float timeStep = TimeStep(gameTime);

            if (godMode)
            {
                OwningObject.ColorMask = new Color(1f, .2f, 0f);
                return;
            }

            if(ledgeClimbing)
            {
                OwningObject.ColorMask = new Color(1f, .6f, 0f);
                velocity = new Vector2(maxSpeed / 3f * (wallSlideSide == Side.Left ? -1f : 1f), -ledgeClimbSpeed);
            }
            else
            {
                if (feelsGravity)
                {
                    velocity += Vector2.UnitY * gravityStrength * timeStep;
                }

                if (Grounded)
                {
                    OwningObject.ColorMask = new Color(.2f, .8f, .2f);
                }
                else if (wallSliding)
                {
                    OwningObject.ColorMask = new Color(1f, 1f, 0f);
                    velocity.X = .5f * (wallSlideSide == Side.Left ? -1f : 1f);
                    if (velocity.Y > maxWallSlideSpeed)
                        velocity.Y = maxWallSlideSpeed;
                    else if (velocity.Y < 0f)
                        velocity.Y += MathF.Min(wallSlideDrag, -velocity.Y);
                }
                else
                {
                    OwningObject.ColorMask = new Color(.3f, 1f, .3f);
                }
            }

            OwningObject.Position += velocity * timeStep;
            if (OwningObject.Position.Y > 10f)
                Kill();
            Grounded = false;
            wallSliding = false;
            ledgeClimbing = false;
        }

        public override void HandleCollision(CollisionInfo collision)
        {
            if (godMode)
                return;

            PhysicsObject other = collision.obj1 == this ? collision.obj2 : collision.obj1;

            Rect currentOverlap = Bounds.Intersect(other.Bounds);

            if(collision.HorizontalSurface)
            {
                if (currentOverlap.Width > PhysicsManager.epsilon && collision.side == Side.Bottom && !Grounded)
                {
                    Grounded = true;
                }
            }
            else
            {
                if (currentOverlap.Height > PhysicsManager.epsilon)
                {
                    if (wallSliding) //Collision on wrong side, probably shouldn't happen
                    {
                        if(collision.side == wallSlideSide)
                        {
                            ledgeHeight = MathF.Max(ledgeHeight, Bounds.Bottom - other.Bounds.Top);
                        }
                        else
                        {
                            Debug.WriteLine("Warning: unexpected state, detected collision on both sides");
                        }
                    }
                    else
                    {
                        wallSliding = true;
                        wallSlideSide = collision.side;
                        ledgeHeight = Bounds.Bottom - other.Bounds.Top;
                    }
                }
            }
            base.HandleCollision(collision);
        }

        protected void HandleInput(GameTime gameTime)
        {
            float timeStep = TimeStep(gameTime);

            if(Controls.GetPressed(ControlID.GodMode))
            {
                if(godMode)
                {
                    //exit godmode
                    godMode = false;
                    Grounded = false;
                    wallSliding = false;
                    velocity = Vector2.Zero;
                }
                else if(enableGodMode)
                {
                    //enter godmode
                    godMode = true;
                }
            }
            if(godMode)
            {
                if (Controls.GetState(ControlID.Left))
                    OwningObject.Position.X -= maxSpeed * timeStep;
                if (Controls.GetState(ControlID.Right))
                    OwningObject.Position.X += maxSpeed * timeStep;
                if (Controls.GetState(ControlID.Up))
                    OwningObject.Position.Y -= maxSpeed * timeStep;
                if (Controls.GetState(ControlID.Down))
                    OwningObject.Position.Y += maxSpeed * timeStep;
                return;
            }

            if(Grounded)
            {
                int horizontalAccelDir = 0;
                if (Controls.GetState(ControlID.Left))
                {
                    --horizontalAccelDir;
                }
                if(Controls.GetState(ControlID.Right))
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

                wallSliding = false;

                if(Controls.GetState(ControlID.Jump))
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
                if (Controls.GetState(ControlID.Down))
                {
                    wallJumpCharge = 0f;
                    wallSliding = false;
                }
                else if(Controls.GetState(ControlID.Up) && ledgeHeight <= OwningObject.ObjectSize.Y * ledgeClimbRatio)
                {
                    wallJumpCharge = 0f;
                    ledgeClimbing = true;
                }
                else if(Controls.GetState(ControlID.Jump))
                {
                    wallJumpCharge += timeStep;
                }
                else
                {
                    if(wallJumpCharge > 0f)
                    {
                        wallSliding = false;
                        if (wallJumpCharge > maxWallJumpCharge)
                            wallJumpCharge = maxWallJumpCharge;
                        float wallJumpSpeed = minWallJumpSpeed + (maxWallJumpSpeed - minWallJumpSpeed) * (wallJumpCharge / maxWallJumpCharge);
                        float wallJumpDirection;
                        if (wallSlideSide == Side.Left)
                            wallJumpDirection = 1f;
                        else
                            wallJumpDirection = -1f;
                        velocity = Vector2.Normalize(new(wallJumpDirection, -wallJumpRatio)) * wallJumpSpeed;
                    }
                    wallJumpCharge = 0f;
                }
            }
            else
            {
                wallJumpCharge = 0f;
            }
        }

        public void Kill()
        {
            OwningObject.Position = Vector2.Zero;
            velocity = Vector2.Zero;
            jumpCharge = 0f;
            wallJumpCharge = 0f;
        }
    }
}
