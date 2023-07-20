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
    internal class Block:PhysicsObject
    {
        private static readonly Dictionary<int, Texture2D> textures = new();

        public static void SetTexture(int id, Texture2D texture)
        {
            textures[id] = texture;
        }

        protected static Texture2D GetTexture(int id)
        {
            if (!textures.TryGetValue(id, out Texture2D texture))
                return textures[1];
            return texture;
        }
        
        Point location;
        int id;

        public Block(GameObject obj, Point location, int id): base(obj, Type.Fixed)
        {
            this.location = location;
            this.id = id;
            OwningObject.SetTexture(GetTexture(id));
            OwningObject.Position = location.ToVector2();
        }
    }

    internal class Level:ManagedObject
    {
        protected PhysicsManager physicsManager;
        protected Dictionary<Point, Block> blocks = new();
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
            Block.SetTexture(1, content.Load<Texture2D>("block"));
        }

        public Block GetBlock(Point location)
        {
            blocks.TryGetValue(location, out Block block);
            return block;
        }

        public void RemoveBlock(Point location)
        {
            Block block = GetBlock(location);
            if (block != null)
            {
                blocks.Remove(location);
                DestroyGameObject(block.OwningObject);
            }
        }

        public void CreateBlock(Point location, int blockId = 1)
        {
            Block block = new Block(CreateGameObject(), location, blockId);
            physicsManager.AddPhysicsObject(block);
            blocks.Add(location, block);
        }
    }
}
