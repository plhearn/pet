
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlayingGameData
{
    public class CutsceneFrame : ContentObject
    {
        public int frame;
        public string actorName;
        public string animationName;
        public float x;
        public float y;

        public CutsceneFrame()
        {

        }

        public CutsceneFrame(int frame, string actorName, string animationName, float x, float y)
        {
            this.frame = frame;
            this.actorName = actorName;
            this.animationName = animationName;
            this.x = x;
            this.y = y;
        }
    }

    public class Cutscene : WorldObject
    {
        public string name;
        public List<CutsceneFrame> frames = new List<CutsceneFrame>();
        public int currentFrame = 0;
        public int maxFrame = 0;
        public bool allowInput;

        /// <summary>
        /// The Cutscene in the Cutscene, along with quantities.
        /// </summary>
        private List<ContentEntry<CutsceneFrame>> entries = new List<ContentEntry<CutsceneFrame>>();

        /// <summary>
        /// The Cutscene in the Cutscene, along with quantities.
        /// </summary>
        public List<ContentEntry<CutsceneFrame>> Entries
        {
            get { return entries; }
            set { entries = value; }
        }


        /// <summary>
        /// Array accessor for the Cutscene's contents.
        /// </summary>
        public ContentEntry<CutsceneFrame> this[int index]
        {
            get { return entries[index]; }
        }


        /// <summary>
        /// Returns true if the Cutscene is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return ((entries.Count <= 0)); }
        }

        public int setMaxFrame()
        {
            foreach(CutsceneFrame frame in frames)
            {
                if(frame.frame > maxFrame)
                    maxFrame = frame.frame;
            }

            return maxFrame;
        }

        public Cutscene(string name)
        {
            this.name = name;
        }


        #region Content Type Reader

        /*
        /// <summary>
        /// Reads a Chest object from the content pipeline.
        /// </summary>
        public class CutsceneReader : ContentTypeReader<Cutscene>
        {
            protected override Cutscene Read(ContentReader input, Cutscene existingInstance)
            {
                Cutscene cutscene = existingInstance;
                if (cutscene == null)
                {
                    cutscene = new Cutscene();
                }

                return cutscene;
            }
        }

        */
        #endregion

    }
}
