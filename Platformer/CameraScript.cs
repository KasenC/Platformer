using Engine;
using Microsoft.Xna.Framework;

namespace Platformer
{
    internal class FollowPlayerCam: Script<Camera>
    {
        public Player player;

        public FollowPlayerCam(Player player)
        {
            this.player = player;
        }

        public override void DrawUpdate(GameTime gameTime)
        {
            OwningObject.position = player.Center;
        }
    }
}
