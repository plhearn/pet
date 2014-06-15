
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
    public class LightFlashOverlay : MapOverlay
    {
        /*
     ___________
    |     |     |
    |  1  |  2  |
    |-----|-----| 
    |  3  |  4  | 
    |_____|_____| 
         
         */


        public List<int> flashFrames;
        SoundEffect thunder;

        public LightFlashOverlay()
        {
            pane1 = new OverlayPane();
            pane2 = new OverlayPane();
            pane3 = new OverlayPane();
            pane4 = new OverlayPane();

            drift = Vector2.Zero;

            overlays = new List<OverlayPane>();

            flashFrames = new List<int>();
            flashFrames.Add(1100);

            lifeTimer = 0;
        }


        public LightFlashOverlay(Vector2 drift, float opacity, Texture2D tex, int width, int height, SoundEffect song)
        {
            this.drift = drift;
            this.opacity = opacity;

            pane1 = new OverlayPane();
            pane1.TextureName = "pane1";
            pane1.FramesPerRow = 1;
            pane1.FrameDimensions = new Point(pane1.width, pane1.height);
            pane1.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane1.Texture = tex;
            pane1.position = new Vector2(-9999, -9999);

            overlays = new List<OverlayPane>();
            overlays.Add(pane1);

            flashFrames = new List<int>();

            flashFrames.Add(1100);

            lifeTimer = 0;

            thunder = song;
        }


        public override void Update(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            lifeTimer++;

            if (lifeTimer > 1500)
                lifeTimer = 0;

            pane1.position = new Vector2(-9999, -9999);

            for (int i = 0; i < flashFrames.Count; i++)
            {
                if (lifeTimer == flashFrames[i])
                {
                    pane1.position = Vector2.Zero;
                    thunder.Play();
                }
            }
        }


        
   }
}
