using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using System.Reflection.Metadata;

namespace Platformer
{
    internal enum ControlID { Left, Right, Up, Down, Jump, LeftClick, RightClick, GodMode, Save };

    public class MyGame : GameEngine
    {
        PhysicsManager physicsManager;
        LevelManager levelManager;
        Player player;

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
            controls.Add(ControlID.Save, new KeyControl(Keys.P));
            
            physicsManager = new(this, 10f);
            levelManager = new(this, physicsManager);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            //Player
            player = new Player(new GameObject(this));
            physicsManager.AddPhysicsObject(player);
            new FollowPlayerCam(camera, player);

            //Level
            Block.LoadTextures(Content);
            levelManager.LoadContent();

            //Test
            //GameObject testObj = new GameObject(this);
            //testObj.AddManaged(new Script<GameObject>(testObj));

            base.LoadContent();
        }
    }

    internal class LevelManager:ManagedObject
    {
        Level level;
        PhysicsManager physicsManager;
        public bool enableClickBlockCreation = true;
        public int blockIdToCreate = 1;

        public LevelManager(IGameObjectManager manager, PhysicsManager physicsManager): base(manager)
        {
            this.physicsManager = physicsManager;
            level = new Level(Manager, physicsManager);
        }

        public void LoadContent()
        {
            if (!level.LoadOrNew("save/level.lvl"))
            {
                for (int i = 0; i < 11; ++i)
                    level.CreateBlock(new(i - 7, 0));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (enableClickBlockCreation)
            {
                Point mousePos = Vector2.Floor(Controls.MouseWorldPos).ToPoint();
                if (Controls.GetPressed(ControlID.RightClick))
                {
                    level.RemoveBlock(mousePos);
                }
                else if (Controls.GetPressed(ControlID.LeftClick))
                {
                    level.CreateBlock(mousePos, blockIdToCreate);
                }
            }
            if (Controls.GetPressed(ControlID.Save))
            {
                level.Save();
            }
        }
    }
}