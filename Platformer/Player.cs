﻿using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platformer
{
    internal class Player:PhysicsObject
    {
        public float maxSpeed = 5f, jumpSpeed = 5f, //in units/s
            acceleration = 10f; //in units/s^2

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
            gameObject.CenterPos = CenterPos.BottomMiddle;
        }

        public override void Update(GameTime gameTime)
        {
            if(Grounded)
                gameObject.ColorMask = Color.LightGray;
            else
                gameObject.ColorMask = Color.White;
            HandleInput(gameTime);

            base.Update(gameTime);
            Grounded = false; 
        }

        protected void HandleInput(GameTime gameTime)
        {
            float timeStep = TimeStep(gameTime);

            if(Grounded)
            {
                int horizontalAccelDir = 0;
                if(Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    --horizontalAccelDir;
                }
                if(Keyboard.GetState().IsKeyDown(Keys.D))
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

                if(Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    velocity.Y = -jumpSpeed;
                }
            }
        }

        public override void HandleCollision(CollisionInfo collision)
        {
            PhysicsObject other = collision.obj1 == this ? collision.obj2 : collision.obj1;

            if (!collisionBox.Intersects(other.collisionBox))
                return;

            if (collision.surface == Side.Bottom)
            {
                if (Grounded)
                    return;
                Grounded = true;
                PositionSide(Side.Bottom, other.collisionBox.Top + PhysicsManager.precisionOffset);
                velocity.Y = 0f;
            }
            else if (collision.surface == Side.Top)
            {
                PositionSide(Side.Top, other.collisionBox.Bottom + PhysicsManager.precisionOffset);
                velocity.Y = 0f;
            }
            else if(!collision.HorizontalSurface)
            {
                if (collisionBox.Intersect(other.collisionBox).Height < 2 * PhysicsManager.precisionOffset)
                    return;

                if (collision.surface == Side.Left)
                {
                    PositionSide(Side.Left, other.collisionBox.Right + PhysicsManager.precisionOffset);
                }
                else
                {
                    PositionSide(Side.Right, other.collisionBox.Left - PhysicsManager.precisionOffset);
                }
                velocity.X = 0f;
                Debug.WriteLine("Horizontal velocity cancelled");
            }
        }
    }
}
