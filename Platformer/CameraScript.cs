using Engine;
using Microsoft.Xna.Framework;

namespace Platformer
{
    internal class FollowPlayerCam: Script<Camera>
    {
        public Player player;

        public FollowPlayerCam(Camera cam, Player player) : base(cam)
        {
            this.player = player;
        }

        protected override void DrawUpdate(GameTime gameTime)
        {
            OwningObject.position = player.Center;
        }
    }
}
