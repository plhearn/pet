
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
    public class FadeOverlay : MapOverlay
    {
        /*
     ___________
    |     |     |
    |  1  |  2  |
    |-----|-----| 
    |  3  |  4  | 
    |_____|_____| 
         
         */


        int duration;
        float startOpacity;
        float endOpacity;

        public FadeOverlay()
        {
            pane1 = new OverlayPane();
            pane2 = new OverlayPane();
            pane3 = new OverlayPane();
            pane4 = new OverlayPane();

            drift = Vector2.Zero;

            overlays = new List<OverlayPane>();

            lifeTimer = 0;
            startOpacity = this.opacity;
            endOpacity = this.opacity;
        }


        public FadeOverlay(int duration, float startOpacity, float endOpacity, Texture2D tex, int width, int height)
        {
            this.drift = Vector2.Zero;
            this.opacity = 0.0f;

            pane1 = new OverlayPane();
            pane1.TextureName = "pane1";
            pane1.FramesPerRow = 1;
            pane1.FrameDimensions = new Point(pane1.width, pane1.height);
            pane1.AddAnimation(new Animation("fade", 1, 1, 1, false));
            pane1.Texture = tex;
            pane1.position = Vector2.Zero;

            overlays = new List<OverlayPane>();
            overlays.Add(pane1);

            lifeTimer = 0;
            this.startOpacity = startOpacity;
            this.endOpacity = endOpacity;
            this.duration = duration;

        }


        public override void Update(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            if (lifeTimer == 0)
                opacity = startOpacity;

            if (lifeTimer < duration)
            {
                lifeTimer++;
                opacity += (endOpacity - startOpacity) * (1 / (float)duration);
            }
        }



    }
}
