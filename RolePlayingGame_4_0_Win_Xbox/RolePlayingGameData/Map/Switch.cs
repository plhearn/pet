#region File Description
//-----------------------------------------------------------------------------
// Switch.cs
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
    /// A movable Switch in the game world.
    /// </summary>
    public class Switch : WorldObject
#if WINDOWS
, ICloneable
#endif
    {
        #region Switch Contents

        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }


        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        public bool active;

        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public bool Active
        {
            get { return active; }
            set { active = value; }
        }


        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        public bool alwaysActive;

        /// <summary>
        /// The true position of the Switch.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public bool AlwaysActive
        {
            get { return alwaysActive; }
            set { alwaysActive = value; }
        }

        #endregion


        #region Graphics Data


        /// <summary>
        /// The content name of the texture for this Switch.
        /// </summary>
        private string textureName;

        /// <summary>
        /// The content name of the texture for this Switch.
        /// </summary>
        public string TextureName
        {
            get { return textureName; }
            set { textureName = value; }
        }


        /// <summary>
        /// The texture for this Switch.
        /// </summary>
        private Texture2D onTexture;

        /// <summary>
        /// The texture for this Switch.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D OnTexture
        {
            get { return onTexture; }
            set { onTexture = value; }
        }


        /// <summary>
        /// The texture for this Switch.
        /// </summary>
        private Texture2D offTexture;

        /// <summary>
        /// The texture for this Switch.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D OffTexture
        {
            get { return offTexture; }
            set { offTexture = value; }
        }

        #endregion


        #region Content Type Reader


        /// <summary>
        /// Reads a Switch object from the content pipeline.
        /// </summary>
        public class SwitchReader : ContentTypeReader<Switch>
        {
            protected override Switch Read(ContentReader input,
                Switch existingInstance)
            {
                Switch Switch = existingInstance;
                if (Switch == null)
                {
                    Switch = new Switch();
                }

                input.ReadRawObject<WorldObject>(Switch as WorldObject);

                //Switch.moveType = getMoveType(input.ReadString());

                //Switch.TextureName = input.ReadString();

                /*
                Switch.Texture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Switchs", Switch.TextureName));
                */

                Switch.Position = Vector2.Zero;
                Switch.OnTexture = input.ContentManager.Load<Texture2D>("Textures\\Switches\\SwitchOn");
                Switch.OffTexture = input.ContentManager.Load<Texture2D>("Textures\\Switches\\SwitchOff");

                return Switch;
            }

        }


        #endregion


        #region ICloneable Members


        /// <summary>
        /// Clone implementation for Switch copies.
        /// </summary>
        /// <remarks>
        /// The game has to handle Switchs that have had some contents removed
        /// without modifying the original Switch (and all Switchs that come after).
        /// </remarks>
        public object Clone()
        {
            // create the new Switch
            Switch Switch = new Switch();

            // copy the data
            Switch.Name = Name;
            Switch.OnTexture = OnTexture;
            Switch.OffTexture = OffTexture;
            Switch.TextureName = TextureName;
            Switch.Active = Active;
            Switch.AlwaysActive = AlwaysActive;
            Switch.Position = Position;

            return Switch;
        }


        #endregion
    }
}
