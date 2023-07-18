using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platformer
{
    public class MyGame : GameEngine
    {
        PhysicsManager physicsManager;
        Player player;
        PhysicsObject platform;
        List<PhysicsObject> blocks = new();

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1800;
            graphics.PreferredBackBufferHeight = 1000;
            graphics.ApplyChanges();
            pixelsPerWorldUnit = 50f;
            drawOnPixelGrid = true;
            
            physicsManager = new(10f);
            AddManagedObject(physicsManager);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            //Player
            player = new Player(CreateGameObject());
            physicsManager.AddPhysicsObject(player);
            player.owningObject.SetTexture(Content.Load<Texture2D>("Player"));
            player.owningObject.Position = new Vector2(0f, 0f);
            camera.AddScript(new FollowPlayerCam(player));

            //Platform
            platform = new PhysicsObject(CreateGameObject(), PhysicsObject.Type.Fixed);
            physicsManager.AddPhysicsObject(platform);
            platform.owningObject.SetTexture(Content.Load<Texture2D>("400pxPlatform"));
            platform.owningObject.Position = new Vector2(0f, 0f);

            //Blocks
            Texture2D blockTex = Content.Load<Texture2D>("block");
            void createBlock(float x, float y)
            {
                PhysicsObject block = new PhysicsObject(CreateGameObject(), PhysicsObject.Type.Fixed);
                physicsManager.AddPhysicsObject(block);
                block.owningObject.SetTexture(blockTex);
                block.owningObject.Position = new Vector2(x, y);
                blocks.Add(block);
            }
            for (int i = 0; i < 7; ++i)
                createBlock(i - 7f, 0f);
            for (int i = 0; i < 6; ++i)
                createBlock(-8f, -i);


            base.LoadContent();
        }
    }

    public abstract class Control
    {
        public abstract bool GetState();
    }

    public class KeyControl : Control
    {
        public List<Keys> keys;

        public KeyControl(Keys key)
        {
            keys = new() { key };
        }

        public KeyControl(List<Keys> keys)
        {
            this.keys = keys;
        }

        public override bool GetState()
        {
            return Keyboard.GetState().GetPressedKeys().Any(x => keys.Contains(x));
        }
    }

    internal static class Controls
    {
        public static Control
            left = new KeyControl(Keys.A),
            right = new KeyControl(Keys.D),
            jump = new KeyControl(Keys.Space)
        ;
    }
}