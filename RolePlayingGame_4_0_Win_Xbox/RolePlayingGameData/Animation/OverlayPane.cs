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
    public class OverlayPane : AnimatingSprite
#if WINDOWS
, ICloneable
#endif
    {
        public int rowOffset = 0;
        public Vector2 position = Vector2.Zero;
        public int width = 1024;
        public int height = 768;
        public int fallSpeed = 0;
        public int driftSpeed = 0;

        #region Updating


        private static Random random = new Random();

        /// <summary>
        /// Update the current animation.
        /// </summary>
        public virtual void UpdateOverlayPane(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            base.UpdateAnimation(elapsedSeconds);

            position.Y += fallSpeed;
            position.X -= driftSpeed;

            if (position.X < viewportWidth)
                position.X += viewportWidth * 2;

            if (position.X > viewportWidth)
                position.X -= viewportWidth * 2;

            if (position.Y < viewportHeight)
                position.Y += viewportHeight * 2;

            if (position.Y > viewportHeight)
                position.Y -= viewportHeight * 2;


            position += movement;
        }


        #endregion

    }
}
