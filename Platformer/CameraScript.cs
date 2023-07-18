using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platformer
{
    internal class FollowPlayerCam: Script<Camera>
    {
        public GameObject player;

        public FollowPlayerCam(GameObject player)
        {
            this.player = player;
        }

        public override void Update(GameTime gameTime)
        {
            owningObject.position = player.Position;
        }
    }
}
