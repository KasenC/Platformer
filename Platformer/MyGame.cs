using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine;
using System;

namespace Platformer
{
    public class MyGame : GameEngine
    {
        PhysicsManager physicsManager;
        Player player;
        PhysicsObject platform;

        protected override void Initialize()
        {
            pixelsPerWorldUnit = 50f;
            drawOnPixelGrid = true;
            physicsManager = new(10f);
            AddManagedObject(physicsManager);
            player = new Player(CreateGameObject());
            physicsManager.AddPhysicsObject(player);
            platform = new PhysicsObject(CreateGameObject(), PhysicsObject.Type.Fixed);
            physicsManager.AddPhysicsObject(platform);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            player.gameObject.SetTexture(Content.Load<Texture2D>("Player"));
            platform.gameObject.SetTexture(Content.Load<Texture2D>("400pxPlatform"), CenterPos.TopMiddle);
            platform.gameObject.Position = new Vector2(0f, 3f);

            base.LoadContent();
        }
    }


}