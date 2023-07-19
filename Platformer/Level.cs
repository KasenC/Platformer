using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platformer
{
    internal class Level:ManagedObject
    {
        protected PhysicsManager physicsManager;
        protected List<PhysicsObject> blocks = new();
        protected Dictionary<int, Texture2D> blockTextures = new();
        internal bool enableClickBlockCreation = false;
        internal int blockIdToCreate = 1;

        public Level(PhysicsManager physicsManager)
        {
            this.physicsManager = physicsManager;
        }

        public override void Update(GameTime gameTime)
        {
            if(enableClickBlockCreation)
            {
                Point mousePos = Vector2.Floor(Controls.MouseWorldPos).ToPoint();
                if (Controls.GetPressed(ControlID.RightClick))
                {
                    RemoveBlock(mousePos);
                }
                else if(Controls.GetPressed(ControlID.LeftClick))
                {
                    CreateBlock(mousePos, blockIdToCreate);
                }
            }
        }

        public void LoadContent(ContentManager content)
        {
            blockTextures.Add(1, content.Load<Texture2D>("block"));
        }

        public PhysicsObject GetBlock(Point position)
        {
            return blocks.Find(x => (x.OwningObject.Position - position.ToVector2()).LengthSquared() < PhysicsManager.precisionOffset);
        }

        public void CreateBlock(int x, int y, int blockId = 1)
        {
            PhysicsObject block = new PhysicsObject(CreateGameObject(), PhysicsObject.Type.Fixed);
            physicsManager.AddPhysicsObject(block);
            Texture2D blockTex;
            if (!blockTextures.TryGetValue(blockId, out blockTex))
                blockTex = blockTextures[1];
            block.OwningObject.SetTexture(blockTex);
            block.OwningObject.Position = new Vector2(x, y);
            blocks.Add(block);
        }

        public void RemoveBlock(Point position)
        {
            PhysicsObject block = GetBlock(position);
            if (block != null)
            {
                blocks.Remove(block);
                RemoveGameObject(block.OwningObject);
            }
        }

        public void CreateBlock(Point position, int blockId = 1)
        {
            CreateBlock(position.X, position.Y, blockId);
        }
    }
}
