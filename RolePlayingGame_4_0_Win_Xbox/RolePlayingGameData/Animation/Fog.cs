#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace RolePlayingGameData
{
    /// <summary>
    /// A sprite sheet with flipbook-style animations.
    /// </summary>
    public class Fog : AnimatingSprite
#if WINDOWS
, ICloneable
#endif
    {
        public int lifeTimer = 0;
        public int rowOffset = 0;
        public Vector2 position = Vector2.Zero;
        public int width = 256;
        public int fallSpeed = -1;
        public int driftSpeed = 1;

        #region Updating


        private static Random random = new Random();

        /// <summary>
        /// Update the current animation.
        /// </summary>
        public void UpdateFog(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            base.UpdateAnimation(elapsedSeconds);

            position.Y += fallSpeed;
            position.X -= driftSpeed;

            position += movement;
        }


        #endregion

    }
}
