#region File Description
//-----------------------------------------------------------------------------
// AnimatingSprite.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

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
    public class Raindrop : AnimatingSprite
#if WINDOWS
, ICloneable
#endif
    {
        public int lifeTimer = 0;
        public int rowOffset = 0;
        public Vector2 position = Vector2.Zero;
        public int width = 16;
        public int fallSpeed = 10;
        public int driftSpeed = 3;

        #region Updating


        private static Random random = new Random();

        /// <summary>
        /// Update the current animation.
        /// </summary>
        public void UpdateRainAnimation(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            base.UpdateAnimation(elapsedSeconds);

            lifeTimer++;

            int padx = 0;
            int pady = 0;

            if (movement.X > 0)
                padx = -300;

            if (movement.X < 0)
                padx = 300;

            if (movement.Y > 0)
                pady = -300;

            if (movement.Y < 0)
                pady = 300;

            if (lifeTimer == 100)
            {
                lifeTimer = 0;
                rowOffset = 0;
                position = new Vector2(random.Next(viewportWidth * 2) - (viewportWidth / 2) + padx ,
                                        random.Next(viewportHeight * 2) - (viewportHeight / 2) + pady);
            }

            if (lifeTimer == 85)
                rowOffset += width;

            if (lifeTimer == 90)
                rowOffset += width;

            if (lifeTimer == 95)
                rowOffset += width;

            if (lifeTimer < 85)
            {
                position.Y += fallSpeed;
                position.X -= driftSpeed;
            }

            position += movement;
        }


        #endregion

    }
}
