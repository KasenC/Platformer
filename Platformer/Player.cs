using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platformer
{
    internal class Player:PhysicsObject
    {
        public Player(GameObject obj) : base(obj, Type.Dynamic, true)
        { }


    }
}
