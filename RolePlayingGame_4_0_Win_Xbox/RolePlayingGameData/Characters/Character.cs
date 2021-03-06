#region File Description
//-----------------------------------------------------------------------------
// Character.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace RolePlayingGameData
{
    /// <summary>
    /// A character in the game world.
    /// </summary>
#if !XBOX
    [DebuggerDisplay("Name = {name}")]
#endif
    public abstract class Character : WorldObject
    {
        #region Character State


        /// <summary>
        /// The state of a character.
        /// </summary>
        public enum CharacterState
        {
            /// <summary>
            /// Ready to perform an action, and playing the idle animation
            /// </summary>
            Idle,

            /// <summary>
            /// Walking in the world.
            /// </summary>
            Walking,

            /// <summary>
            /// In defense mode
            /// </summary>
            Defending,

            /// <summary>
            /// Performing Dodge Animation
            /// </summary>
            Dodging,

            /// <summary>
            /// Performing Hit Animation
            /// </summary>
            Hit,

            /// <summary>
            /// Dead, but still playing the dying animation.
            /// </summary>
            Dying,

            /// <summary>
            /// Dead, with the dead animation.
            /// </summary>
            Dead,
        }


        /// <summary>
        /// The state of this character.
        /// </summary>
        private CharacterState state = CharacterState.Idle;

        /// <summary>
        /// The state of this character.
        /// </summary>
        [ContentSerializerIgnore]
        public CharacterState State
        {
            get { return state; }
            set { state = value; }
        }


        /// <summary>
        /// Returns true if the character is dead or dying.
        /// </summary>
        public bool IsDeadOrDying
        {
            get
            {
                return ((State == CharacterState.Dying) || 
                    (State == CharacterState.Dead));
            }
        }


        #endregion


        #region Map Data


        /// <summary>
        /// The position of this object on the map.
        /// </summary>
        private Point mapPosition;

        /// <summary>
        /// The position of this object on the map.
        /// </summary>
        [ContentSerializerIgnore]
        public Point MapPosition
        {
            get { return mapPosition; }
            set { mapPosition = value; }
        }


        /// <summary>
        /// The orientation of this object on the map.
        /// </summary>
        private Direction direction;

        /// <summary>
        /// The orientation of this object on the map.
        /// </summary>
        [ContentSerializerIgnore]
        public Direction Direction
        {
            get { return direction; }
            set { direction = value; }
        }


        #endregion


        #region Graphics Data


        /// <summary>
        /// The animating sprite for the map view of this character.
        /// </summary>
        private AnimatingSprite mapSprite;

        /// <summary>
        /// The animating sprite for the map view of this character.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public AnimatingSprite MapSprite
        {
            get { return mapSprite; }
            set { mapSprite = value; }
        }


        /// <summary>
        /// The animating sprite for the map view of this character as it walks.
        /// </summary>
        /// <remarks>
        /// If this object is null, then the animations are taken from MapSprite.
        /// </remarks>
        private AnimatingSprite walkingSprite;

        /// <summary>
        /// The animating sprite for the map view of this character as it walks.
        /// </summary>
        /// <remarks>
        /// If this object is null, then the animations are taken from MapSprite.
        /// </remarks>
        [ContentSerializer(Optional=true)]
        public AnimatingSprite WalkingSprite
        {
            get { return walkingSprite; }
            set { walkingSprite = value; }
        }


        /// <summary>
        /// Reset the animations for this character.
        /// </summary>
        public virtual void ResetAnimation(bool isWalking)
        {
            state = isWalking ? CharacterState.Walking : CharacterState.Idle;
            if (mapSprite != null)
            {
                if (isWalking && mapSprite["Walk" + Direction.ToString()] != null)
                {
                    mapSprite.PlayAnimation("Walk", Direction);
                }
                else
                {
                    mapSprite.PlayAnimation("Idle", Direction);

                    //draw still NPCs
                    if (mapSprite.FramesPerRow == 1)
                        mapSprite.PlayOtherAnimation("Still");
                }
            }
            if (walkingSprite != null)
            {
                if (isWalking && walkingSprite["Walk" + Direction.ToString()] != null)
                {
                    walkingSprite.PlayAnimation("Walk", Direction);
                }
                else
                {
                    walkingSprite.PlayAnimation("Idle", Direction);
                }
            }
        }


        /// <summary>
        /// The small blob shadow that is rendered under the characters.
        /// </summary>
        private Texture2D shadowTexture;

        /// <summary>
        /// The small blob shadow that is rendered under the characters.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D ShadowTexture
        {
            get { return shadowTexture; }
            set { shadowTexture = value; }
        }



        #endregion


        #region Standard Animation Data


        /// <summary>
        /// The default idle-animation interval for the animating map sprite.
        /// </summary>
        private int mapIdleAnimationInterval = 200;

        /// <summary>
        /// The default idle-animation interval for the animating map sprite.
        /// </summary>
        [ContentSerializer(Optional=true)]
        public int MapIdleAnimationInterval
        {
            get { return mapIdleAnimationInterval; }
            set { mapIdleAnimationInterval = value; }
        }


        /// <summary>
        /// Add the standard character idle animations to this character.
        /// </summary>
        private void AddStandardCharacterIdleAnimations()
        {
            if (mapSprite != null)
            {
                mapSprite.AddAnimation(new Animation("IdleSouth", 2, 2, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleSouthwest", 2, 2, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleWest", 10, 10, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleNorthwest", 6, 6, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleNorth", 6, 6, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleNortheast", 6, 6, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleEast", 14, 14, 
                    MapIdleAnimationInterval, true));
                mapSprite.AddAnimation(new Animation("IdleSoutheast", 2, 2, 
                    MapIdleAnimationInterval, true));


                mapSprite.AddAnimation(new Animation("Still", 1, 1,
                    MapIdleAnimationInterval, false));
            }
        }


        /// <summary>
        /// The default walk-animation interval for the animating map sprite.
        /// </summary>
        private int mapWalkingAnimationInterval = 160;

        /// <summary>
        /// The default walk-animation interval for the animating map sprite.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int MapWalkingAnimationInterval
        {
            get { return mapWalkingAnimationInterval; }
            set { mapWalkingAnimationInterval = value; }
        }


        /// <summary>
        /// Add the standard character walk animations to this character.
        /// </summary>
        private void AddStandardCharacterWalkingAnimations()
        {
            AnimatingSprite sprite = (walkingSprite == null ? mapSprite : walkingSprite);
            if (sprite != null)
            {
                sprite.AddAnimation(new Animation("WalkSouth", 1, 4, 
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkSouthwest", 1, 4, 
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkWest", 9, 12, 
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkNorthwest", 5, 8,
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkNorth", 5, 8,
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkNortheast", 5, 8, 
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkEast", 13, 16,
                    MapWalkingAnimationInterval, true));
                sprite.AddAnimation(new Animation("WalkSoutheast", 1, 4, 
                    MapWalkingAnimationInterval, true));
            }
        }


        #endregion


        #region Content Type Reader


        /// <summary>
        /// Reads a Character object from the content pipeline.
        /// </summary>
        public class CharacterReader : ContentTypeReader<Character>
        {
            /// <summary>
            /// Reads a Character object from the content pipeline.
            /// </summary>
            protected override Character Read(ContentReader input, 
                Character existingInstance)
            {
                Character character = existingInstance;
                if (character == null)
                {
                    throw new ArgumentNullException("existingInstance");
                }

                input.ReadRawObject<WorldObject>(character as WorldObject);

                character.MapIdleAnimationInterval = input.ReadInt32();
                character.MapSprite = input.ReadObject<AnimatingSprite>();
                if (character.MapSprite != null)
                {
                    character.MapSprite.SourceOffset =
                        new Vector2(
                        character.MapSprite.SourceOffset.X - 32,
                        character.MapSprite.SourceOffset.Y - 32);
                }
                character.AddStandardCharacterIdleAnimations();

                character.MapWalkingAnimationInterval = input.ReadInt32();
                character.WalkingSprite = input.ReadObject<AnimatingSprite>();
                if (character.WalkingSprite != null)
                {
                    character.WalkingSprite.SourceOffset =
                        new Vector2(
                        character.WalkingSprite.SourceOffset.X - 32,
                        character.WalkingSprite.SourceOffset.Y - 32);
                }
                character.AddStandardCharacterWalkingAnimations();

                character.ResetAnimation(false);

                character.shadowTexture = input.ContentManager.Load<Texture2D>(
                    @"Textures\Characters\CharacterShadow");

                return character;
            }
        }


        #endregion
    }
}
