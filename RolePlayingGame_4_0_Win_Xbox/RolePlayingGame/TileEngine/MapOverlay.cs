
#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RolePlayingGameData;
#endregion

namespace RolePlaying
{
    public class MapOverlay
    {
        /*
     ___________
    |     |     |
    |  1  |  2  |
    |-----|-----| 
    |  3  |  4  | 
    |_____|_____| 
         
         */

        public string name;

        public OverlayPane pane1;
        public OverlayPane pane2;
        public OverlayPane pane3;
        public OverlayPane pane4;

        public Vector2 drift;
        public float opacity;
        public List<OverlayPane> overlays;

        public int lifeTimer = 0;
        public int duration;

        public bool deactivated;

        public MapOverlay()
        {
            pane1 = new OverlayPane();
            pane2 = new OverlayPane();
            pane3 = new OverlayPane();
            pane4 = new OverlayPane();

            drift = Vector2.Zero;

            overlays = new List<OverlayPane>();
        }


        public MapOverlay(Vector2 drift, float opacity, Texture2D tex, int width, int height)
        {
            this.drift = drift;
            this.opacity = opacity;

            pane1 = new OverlayPane();
            pane1.TextureName = "pane1";
            pane1.FramesPerRow = 1;
            pane1.FrameDimensions = new Point(pane1.width, pane1.height);
            pane1.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane1.Texture = tex;
            pane1.position = new Vector2(0, 0);

            pane2 = new OverlayPane();
            pane2.TextureName = "pane2";
            pane2.FramesPerRow = 1;
            pane2.FrameDimensions = new Point(pane2.width, pane2.height);
            pane2.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane2.Texture = tex;
            pane2.position = new Vector2(width, 0);

            pane3 = new OverlayPane();
            pane3.TextureName = "pane3";
            pane3.FramesPerRow = 1;
            pane3.FrameDimensions = new Point(pane3.width, pane3.height);
            pane3.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane3.Texture = tex;
            pane3.position = new Vector2(0, height);

            pane4 = new OverlayPane();
            pane4.TextureName = "pane4";
            pane4.FramesPerRow = 1;
            pane4.FrameDimensions = new Point(pane4.width, pane4.height);
            pane4.AddAnimation(new Animation("fog", 1, 1, 1, false));
            pane4.Texture = tex;
            pane4.position = new Vector2(width, height);


            overlays = new List<OverlayPane>();
            overlays.Add(pane1);
            overlays.Add(pane2);
            overlays.Add(pane3);
            overlays.Add(pane4);
        }


        public virtual void Update(float elapsedSeconds, int viewportWidth, int viewportHeight, Vector2 movement)
        {
            lifeTimer++;

            if (lifeTimer > 100000)
                lifeTimer = 0;

            pane1.UpdateOverlayPane(elapsedSeconds, viewportWidth, viewportHeight, movement);
            pane2.UpdateOverlayPane(elapsedSeconds, viewportWidth, viewportHeight, movement);
            pane3.UpdateOverlayPane(elapsedSeconds, viewportWidth, viewportHeight, movement);
            pane4.UpdateOverlayPane(elapsedSeconds, viewportWidth, viewportHeight, movement);
        }


        
   }
}
