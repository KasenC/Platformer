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
            wallJumpRatio = 1.5f; //Ratio of vertical velocity to horizontal velocity when walljumping

        public bool enableGodMode = false;

        protected float jumpCharge = 0f, wallJumpCharge = 0f;

        protected bool godMode = false;

        protected bool wallSliding, ledgeClimbing;
        protected Side wallSlideSide;
        protected float ledgeHeight;

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
            OwningObject.CenterPos = CenterPos.BottomMiddle;
        }

        public override void Update(GameTime gameTime)
        {
            if(godMode)
            {
                OwningObject.ColorMask = new Color(1f, .2f, 0f);
            }
            else if (Grounded)
            {
                OwningObject.ColorMask = new Color(.2f, .8f, .2f);
                wallSliding = false;
            }
            else if (wallSliding)
                OwningObject.ColorMask = new Color(1f, 1f, 0f);
            else
                OwningObject.ColorMask = new Color(.3f, 1f, .3f);
            
            HandleInput(gameTime);
            if (godMode)
                return;

            float timeStep = TimeStep(gameTime);
            if(ledgeClimbing)
            {
                velocity = new Vector2(ledgeClimbSpeed * (wallSlideSide == Side.Left ? -1f : 1f), -ledgeClimbSpeed);
            }
            else
            {
                if (feelsGravity)
                {
                    velocity += Vector2.UnitY * gravityStrength * timeStep;
                }
                if(wallSliding)
                {
                    if (velocity.Y > maxWallSlideSpeed)
                        velocity.Y = maxWallSlideSpeed;
                    else if (velocity.Y < 0f)
                        velocity.Y += MathF.Min(wallSlideDrag, MathF.Abs(velocity.Y));
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
                if (currentOverlap.Height < 2 * PhysicsManager.precisionOffset)
                    return;
                if (wallSliding && wallSlideSide == collision.side)
                {
                    ledgeHeight = MathF.Max(ledgeHeight, Bounds.Bottom - other.Bounds.Top);
                    return;
                }
                if (!Grounded)
                {
                    wallSliding = true;
                    wallSlideSide = collision.side;
                    ledgeHeight = Bounds.Bottom - other.Bounds.Top;
                }

                if (collision.side == Side.Left)
                {
                    PositionSide(Side.Left, other.Bounds.Right - PhysicsManager.precisionOffset);
                }
                else
                {
                    PositionSide(Side.Right, other.Bounds.Left + PhysicsManager.precisionOffset);
                }
                velocity.X = 0f;
            }
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
                    if(wallSlideSide == Side.Left)
                        OwningObject.Position.X += 2 * PhysicsManager.precisionOffset;
                    else
                        OwningObject.Position.X -= 2 * PhysicsManager.precisionOffset;
                }
                else if(Controls.GetState(ControlID.Up) && ledgeHeight <= OwningObject.ObjectSize.Y * .75f)
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
