#region File Description
//-----------------------------------------------------------------------------
// Block.cs
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

    public enum BlockMoveType
    {
        AlwaysPush = 1,
        Slide = 2,
        OnePush = 3
    }

    
        public enum slideDirection
        {
            up = 1,
            down = 2,
            left = 3,
            right = 4,
            none = 5
        }


    /// <summary>
    /// A movable Block in the game world.
    /// </summary>
    public class Block : WorldObject
#if WINDOWS
, ICloneable
#endif
    {
        #region Block Contents


        /// <summary>
        /// The amount of gold in the Block.
        /// </summary>
        private BlockMoveType moveType;

        /// <summary>
        /// The amount of gold in the Block.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public BlockMoveType MoveType
        {
            get { return moveType; }
            set { moveType = value; }
        }

        /// <summary>
        /// The amount of gold in the Block.
        /// </summary>
        private slideDirection slideDirection = slideDirection.none;

        /// <summary>
        /// The amount of gold in the Block.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public slideDirection SlideDirection
        {
            get { return slideDirection; }
            set { slideDirection = value; }
        }

        /// <summary>
        /// The true position of the Block.
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// The true position of the Block.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        #endregion


        #region Graphics Data


        /// <summary>
        /// The content name of the texture for this Block.
        /// </summary>
        private string textureName;

        /// <summary>
        /// The content name of the texture for this Block.
        /// </summary>
        public string TextureName
        {
            get { return textureName; }
            set { textureName = value; }
        }


        /// <summary>
        /// The texture for this Block.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// The texture for this Block.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }


        #endregion


        #region Content Type Reader


        /// <summary>
        /// Reads a Block object from the content pipeline.
        /// </summary>
        public class BlockReader : ContentTypeReader<Block>
        {
            protected override Block Read(ContentReader input,
                Block existingInstance)
            {
                Block Block = existingInstance;
                if (Block == null)
                {
                    Block = new Block();
                }

                Block.moveType = getMoveType(input.ReadString());

                input.ReadRawObject<WorldObject>(Block as WorldObject);


                //Block.TextureName = input.ReadString();

                /*
                Block.Texture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Blocks", Block.TextureName));
                */
                Block.Position = Vector2.Zero;
                Block.Texture = input.ContentManager.Load<Texture2D>("Textures\\Blocks\\default");

                return Block;
            }

            public BlockMoveType getMoveType(string type)
            {
                BlockMoveType moveType = BlockMoveType.AlwaysPush;

                if (type == "AlwaysPush")
                {
                    moveType = BlockMoveType.AlwaysPush;
                }

                if (type == "Slide")
                {
                    moveType = BlockMoveType.Slide;
                }

                if (type == "OnePush")
                {
                    moveType = BlockMoveType.OnePush;
                }

                return moveType;
            }
        }


        #endregion


        #region ICloneable Members


        /// <summary>
        /// Clone implementation for Block copies.
        /// </summary>
        /// <remarks>
        /// The game has to handle Blocks that have had some contents removed
        /// without modifying the original Block (and all Blocks that come after).
        /// </remarks>
        public object Clone()
        {
            // create the new Block
            Block Block = new Block();

            // copy the data
            Block.MoveType = MoveType;
            Block.Name = Name;
            Block.Texture = Texture;
            Block.TextureName = TextureName;
            Block.Position = Position;

            return Block;
        }


        #endregion
    }
}
