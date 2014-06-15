
#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using RolePlayingGameData;
#endregion

namespace RolePlaying
{
    public class FadeInOverlay : MapOverlay
    {
        /*
     ___________
    |     |     |
    |  1  |  2  |
    |-----|-----| 
    |  3  |  4  | 
    |_____|_____| 
         
         */


        float startOpacity;

        public FadeInOverlay()
        {
            pane1 = new OverlayPane();
            pane2 = new OverlayPane();
            pane3 = new OverlayPane();
            pane4 = new OverlayPane();

            drift = Vector2.Zero;

            overlays = new List<OverlayPane>();

            lifeTimer = 0;
            startOpacity = this.opacity;
        }


        public FadeInOverlay(Vector2 drift, float opacity, Texture2D tex, int width, int height)
        {
            this.drift = drift;
            this.opacity = opacity;

            pane1 = new OverlayPane();
            pane1.TextureName = "pane1";
            pane1.FramesPerRow = 1;
            pane1.FrameDimensions = new Point(pane1.width, pane1.height);
            pane1.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane1.Texture = tex;
            pane1.position = Vector2.Zero;

            overlays = new List<OverlayPane>();
            overlays.Add(pane1);

            lifeTimer = 0;
            startOpacity = opacity;
        }


        public override void Update(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            float fadeTime = 1000.00f;

            if (lifeTimer < fadeTime)
            {
                lifeTimer++;
                opacity = startOpacity * (lifeTimer / fadeTime);

                if (lifeTimer > 100000)
                    lifeTimer = 0;
            }
        }



    }
}
