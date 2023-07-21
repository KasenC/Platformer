using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Platformer
{
    internal class Block:PhysicsObject
    {
        private static readonly Dictionary<int, Texture2D> textures = new();

        public static void LoadTextures(ContentManager content)
        {
            SetTexture(1, content.Load<Texture2D>("block"));
        }

        private static void SetTexture(int id, Texture2D texture)
        {
            textures[id] = texture;
        }

        protected static Texture2D GetTexture(int id)
        {
            if (!textures.TryGetValue(id, out Texture2D texture))
                return textures[1];
            return texture;
        }
        
        public readonly int id;

        public Block(GameObject obj, Point location, int id): base(obj, Type.Fixed)
        {
            this.id = id;
            OwningObject.SetTexture(GetTexture(id));
            OwningObject.Position = location.ToVector2();
        }
    }

    internal class Level: ManagedObject
    {
        protected PhysicsManager physicsManager;
        protected Dictionary<Point, Block> blocks = new();
        public string levelPath;

        public Level(PhysicsManager physicsManager)
        {
            this.physicsManager = physicsManager;
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
            if (blocks.ContainsKey(location))
                return;
            Block block = new Block(CreateGameObject(), location, blockId);
            physicsManager.AddPhysicsObject(block);
            blocks.Add(location, block);
        }

        public void New(string path = "")
        {
            blocks.Clear();
            levelPath = path;
        }

        public void SaveAs(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
            using var file = new StreamWriter(path);
            foreach ((Point location, Block block) in blocks)
            {
                file.WriteLine("{0},{1},{2}", location.X, location.Y, block.id);
            }
        }

        public bool Save()
        {
            if (levelPath == "")
                return false;
            SaveAs(levelPath);
            return true;
        }

        public bool Load(string path)
        {
            if (!File.Exists(path))
                return false;
            New(path);
            foreach(string line in File.ReadLines(path))
            {
                string[] tokens = line.Split(',');
                if (tokens.Length != 3
                    || !int.TryParse(tokens[0], out int x)
                    || !int.TryParse(tokens[1], out int y)
                    || !int.TryParse(tokens[2], out int id))
                    continue;
                CreateBlock(new(x, y), id);
            }
            return true;
        }

        /// <summary>
        /// Load level if it exists, otherwise create new empty level
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if level was loaded, otherwise false</returns>
        public bool LoadOrNew(string path)
        {
            if (Load(path))
                return true;
            New(path);
            return false;
        }
    }
}
