using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platformer
{
    internal enum ControlID { Left, Right, Up, Down, Jump, LeftClick, RightClick, GodMode };

    public class MyGame : GameEngine
    {
        PhysicsManager physicsManager;
        Level level;
        Player player;
        PhysicsObject platform;


        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1800;
            graphics.PreferredBackBufferHeight = 1000;
            graphics.ApplyChanges();
            pixelsPerWorldUnit = 50f;
            drawOnPixelGrid = true;

            controls.Add(ControlID.Left, new KeyControl(Keys.A));
            controls.Add(ControlID.Right, new KeyControl(Keys.D));
            controls.Add(ControlID.Up, new KeyControl(Keys.W));
            controls.Add(ControlID.Down, new KeyControl(Keys.S));
            controls.Add(ControlID.Jump, new KeyControl(Keys.Space));
            controls.Add(ControlID.LeftClick, new MouseButtonControl(MouseButton.Left));
            controls.Add(ControlID.RightClick, new MouseButtonControl(MouseButton.Right));
            controls.Add(ControlID.GodMode, new KeyControl(Keys.G));
            
            physicsManager = new(10f);
            AddManagedObject(physicsManager);
            level = new(physicsManager);
            AddManagedObject(level);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            //Player
            player = new Player(CreateGameObject());
            physicsManager.AddPhysicsObject(player);
            player.OwningObject.SetTexture(Content.Load<Texture2D>("Player"));
            player.OwningObject.Position = new Vector2(0f, 0f);
            camera.AddScript(new FollowPlayerCam(player));
            player.enableGodMode = true;

            //Platform
            //platform = new PhysicsObject(CreateGameObject(), PhysicsObject.Type.Fixed);
            //physicsManager.AddPhysicsObject(platform);
            //platform.owningObject.SetTexture(Content.Load<Texture2D>("400pxPlatform"));
            //platform.owningObject.Position = new Vector2(0f, 0f);

            //Blocks
            level.LoadContent(Content);
            level.enableClickBlockCreation = true;
            for (int i = 0; i < 11; ++i)
                level.CreateBlock(new(i - 7, 0));
            for (int i = 0; i < 6; ++i)
                level.CreateBlock(new(-8, -i));

            //Test
            //GameObject testObj = CreateGameObject();
            //testObj.SetTexture(blockTex, CenterPos.Middle);
            //testObj.AddScript();

            base.LoadContent();
        }
    }
}