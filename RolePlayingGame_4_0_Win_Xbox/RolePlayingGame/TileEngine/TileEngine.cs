#region File Description
//-----------------------------------------------------------------------------
// PlayerPosition.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using RolePlayingGameData;
#endregion

namespace RolePlaying
{
    public struct TileOverride
    {
        public Vector2 position;
        public int layer;
        public int newValue;
    }

    public class TileOverrideTrigger
    {
        public List<MapEntry<RolePlayingGameData.Switch>> switchCheck;
        public List<TileOverride> overrides;
        public string name;
        public string mapName;
        public bool active;
        public bool alwaysActive;

        public TileOverrideTrigger()
        {

        }
    }

    public class CutsceneTrigger
    {
        public List<Point> tiles;
        public string mapName;
        public string npcName;
        public bool activated;
        public bool repeat;
        public Cutscene cutscene;

        public CutsceneTrigger()
        {

        }
    }

    /// <summary>
    /// Static class for a tileable map
    /// </summary>
    static class TileEngine
    {
        #region Map

        public static Vector2 scrollMovement;

        public static Vector2 userMovement;

        /// <summary>
        /// The map being used by the tile engine.
        /// </summary>
        private static Map map = null;

        /// <summary>
        /// The map being used by the tile engine.
        /// </summary>
        public static Map Map
        {
            get { return map; }
        }
        

        /// <summary>
        /// The position of the outside 0,0 corner of the map, in pixels.
        /// </summary>
        public static Vector2 mapOriginPosition;


        /// <summary>
        /// Calculate the screen position of a given map location (in tiles).
        /// </summary>
        /// <param name="mapPosition">A map location, in tiles.</param>
        /// <returns>The current screen position of that location.</returns>
        public static Vector2 GetScreenPosition(Point mapPosition)
        {
            return new Vector2(
                mapOriginPosition.X + mapPosition.X * map.TileSize.X,
                mapOriginPosition.Y + mapPosition.Y * map.TileSize.Y);
        }


        /// <summary>
        /// Set the map in use by the tile engine.
        /// </summary>
        /// <param name="map">The new map for the tile engine.</param>
        /// <param name="portal">The portal the party is entering on, if any.</param>
        public static void SetMap(Map newMap, MapEntry<Portal> portalEntry)
        {
            // check the parameter
            if (newMap == null)
            {
                throw new ArgumentNullException("newMap");
            }

            // assign the new map
            map = newMap;

            // reset the map origin, which will be recalculate on the first update
            mapOriginPosition = Vector2.Zero;

            // move the party to its initial position
            if (portalEntry == null)
            {
                // no portal - use the spawn position
                partyLeaderPosition.TilePosition = map.SpawnMapPosition;
                partyLeaderPosition.TileOffset = Vector2.Zero;
                partyLeaderPosition.Direction = Direction.South;
            }
            else
            {
                // use the portal provided, which may include automatic movement
                partyLeaderPosition.TilePosition = portalEntry.MapPosition;
                partyLeaderPosition.TileOffset = Vector2.Zero;
                partyLeaderPosition.Direction = portalEntry.Direction;
                /*autoPartyLeaderMovement = Vector2.Multiply(
                    new Vector2(map.TileSize.X, map.TileSize.Y), new Vector2(
                    portalEntry.Content.LandingMapPosition.X - 
                        partyLeaderPosition.TilePosition.X,
                    portalEntry.Content.LandingMapPosition.Y - 
                        partyLeaderPosition.TilePosition.Y));*/
            }
        }


        #endregion


        #region Graphics Data


        /// <summary>
        /// The viewport that the tile engine is rendering within.
        /// </summary>
        private static Viewport viewport;

        /// <summary>
        /// The viewport that the tile engine is rendering within.
        /// </summary>
        public static Viewport Viewport
        {
            get { return viewport; }
            set 
            { 
                viewport = value;
                viewportCenter = new Vector2(
                    viewport.X + viewport.Width / 2f,
                    viewport.Y + viewport.Height / 2f);
            }
        }

        
        /// <summary>
        /// The center of the current viewport.
        /// </summary>
        public static Vector2 viewportCenter;


        #endregion


        #region Party


        /// <summary>
        /// The speed of the party leader, in units per second.
        /// </summary>
        /// <remarks>
        /// The movementCollisionTolerance constant should be a multiple of this number.
        /// </remarks>
        public const float partyLeaderMovementSpeed = 3f;


        /// <summary>
        /// The current position of the party leader.
        /// </summary>
        private static PlayerPosition partyLeaderPosition = new PlayerPosition();
        public static PlayerPosition PartyLeaderPosition
        {
            get { return partyLeaderPosition; }
            set { partyLeaderPosition = value; }
        }


        /// <summary>
        /// The automatic movement remaining for the party leader.
        /// </summary>
        /// <remarks>
        /// This is typically used for automatic movement when spawning on a map.
        /// </remarks>
        public static Vector2 autoPartyLeaderMovement = Vector2.Zero;


        /// <summary>
        /// Updates the automatic movement of the party.
        /// </summary>
        /// <returns>The automatic movement for this update.</returns>
        private static Vector2 UpdatePartyLeaderAutoMovement(GameTime gameTime)
        {
            // check for any remaining auto-movement
            if (autoPartyLeaderMovement == Vector2.Zero)
            {
                return Vector2.Zero;
            }

            // get the remaining-movement direction
            Vector2 autoMovementDirection = Vector2.Normalize(autoPartyLeaderMovement);

            // calculate the potential movement vector
            Vector2 movement = Vector2.Multiply(autoMovementDirection, partyLeaderMovementSpeed);

            // limit the potential movement vector by the remaining auto-movement
            movement.X = Math.Sign(movement.X) * MathHelper.Min(Math.Abs(movement.X),
                Math.Abs(autoPartyLeaderMovement.X));
            movement.Y = Math.Sign(movement.Y) * MathHelper.Min(Math.Abs(movement.Y),
                Math.Abs(autoPartyLeaderMovement.Y));
            
            // remove the movement from the total remaining auto-movement
            autoPartyLeaderMovement -= movement;

            return movement;
        }


        /// <summary>
        /// Update the user-controlled movement of the party.
        /// </summary>
        /// <returns>The controlled movement for this update.</returns>
        private static Vector2 UpdateUserMovement(GameTime gameTime)
        {
            Vector2 desiredMovement = Vector2.Zero;

            if (Session.CurrentCutscene == null || Session.CurrentCutscene.allowInput)
            {
                // accumulate the desired direction from user input
                if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterUp))
                {
                    if (CanPartyLeaderMoveUp())
                    {
                        desiredMovement.Y -= partyLeaderMovementSpeed;
                    }
                }
                if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterDown))
                {
                    if (CanPartyLeaderMoveDown())
                    {
                        desiredMovement.Y += partyLeaderMovementSpeed;
                    }
                }
                if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterLeft))
                {
                    if (CanPartyLeaderMoveLeft())
                    {
                        desiredMovement.X -= partyLeaderMovementSpeed;
                    }
                }
                if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterRight))
                {
                    if (CanPartyLeaderMoveRight())
                    {
                        desiredMovement.X += partyLeaderMovementSpeed;
                    }
                }

                // if there is no desired movement, then we can't determine a direction
                if (desiredMovement == Vector2.Zero)
                {
                    return Vector2.Zero;
                }
            }

            return desiredMovement;
        }


        #endregion


        #region Collision


        /// <summary>
        /// The number of pixels that characters should be allowed to move into 
        /// blocking tiles.
        /// </summary>
        /// <remarks>
        /// The partyMovementSpeed constant should cleanly divide this number.
        /// </remarks>
        const int movementCollisionTolerance = 12;


        /// <summary>
        /// Returns true if the player can move up from their current position.
        /// </summary>
        private static bool CanPartyLeaderMoveUp()
        {
            // if they're not within the tolerance of the next tile, then this is moot
            if (partyLeaderPosition.TileOffset.Y > -movementCollisionTolerance)
            {
                return true;
            }

            // if the player is at the outside left and right edges, 
            /* then check the diagonal tiles
            if (partyLeaderPosition.TileOffset.X < -movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X - 1,
                    partyLeaderPosition.TilePosition.Y - 1)))
                {
                    return false;
                }
            }
            else if (partyLeaderPosition.TileOffset.X > movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X + 1,
                    partyLeaderPosition.TilePosition.Y - 1)))
                {
                    return false;
                }
            }
            */


            foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
            {
                if (TileOverrideTrigger.mapName == Map.Name &&
                    TileOverrideTrigger.active)
                {
                    foreach (TileOverride over in TileOverrideTrigger.overrides)
                    {
                        if (over.position.X == partyLeaderPosition.TilePosition.X &&
                            over.position.Y == partyLeaderPosition.TilePosition.Y-1 &&
                            over.layer == 4)
                        {
                            if (over.newValue == 0)
                                return true;
                            else
                                return false;
                        }
                    }
                }
            }


            // check the tile above the current one
            return !map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X,
                    partyLeaderPosition.TilePosition.Y - 1));



        }


        /// <summary>
        /// Returns true if the player can move down from their current position.
        /// </summary>
        private static bool CanPartyLeaderMoveDown()
        {
            // if they're not within the tolerance of the next tile, then this is moot
            if (partyLeaderPosition.TileOffset.Y < movementCollisionTolerance)
            {
                return true;
            }

            // if the player is at the outside left and right edges, 
            /* then check the diagonal tiles
            if (partyLeaderPosition.TileOffset.X < -movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X - 1,
                    partyLeaderPosition.TilePosition.Y + 1)))
                {
                    return false;
                }
            }
            else if (partyLeaderPosition.TileOffset.X > movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X + 1,
                    partyLeaderPosition.TilePosition.Y + 1)))
                {
                    return false;
                }
            }
            */


            foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
            {
                if (TileOverrideTrigger.mapName == Map.Name &&
                    TileOverrideTrigger.active)
                {
                    foreach (TileOverride over in TileOverrideTrigger.overrides)
                    {
                        if (over.position.X == partyLeaderPosition.TilePosition.X &&
                            over.position.Y == partyLeaderPosition.TilePosition.Y+1 &&
                            over.layer == 4)
                        {
                            if (over.newValue == 0)
                                return true;
                            else
                                return false;
                        }
                    }
                }
            }


            // check the tile below the current one
            return !map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X,
                    partyLeaderPosition.TilePosition.Y + 1));
        }


        /// <summary>
        /// Returns true if the player can move left from their current position.
        /// </summary>
        private static bool CanPartyLeaderMoveLeft()
        {
            // if they're not within the tolerance of the next tile, then this is moot
            if (partyLeaderPosition.TileOffset.X > -movementCollisionTolerance)
            {
                return true;
            }

            // if the player is at the outside left and right edges, 
            /* then check the diagonal tiles
            if (partyLeaderPosition.TileOffset.Y < -movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X - 1,
                    partyLeaderPosition.TilePosition.Y - 1)))
                {
                    return false;
                }
            }
            else if (partyLeaderPosition.TileOffset.Y > movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X - 1,
                    partyLeaderPosition.TilePosition.Y + 1)))
                {
                    return false;
                }
            }
            */

            foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
            {
                if (TileOverrideTrigger.mapName == Map.Name &&
                    TileOverrideTrigger.active)
                {
                    foreach (TileOverride over in TileOverrideTrigger.overrides)
                    {
                        if (over.position.X == partyLeaderPosition.TilePosition.X-1 &&
                            over.position.Y == partyLeaderPosition.TilePosition.Y &&
                            over.layer == 4)
                        {
                            if (over.newValue == 0)
                                return true;
                            else
                                return false;
                        }
                    }
                }
            }

            // check the tile to the left of the current one
            return !map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X - 1,
                    partyLeaderPosition.TilePosition.Y));
        }


        /// <summary>
        /// Returns true if the player can move right from their current position.
        /// </summary>
        private static bool CanPartyLeaderMoveRight()
        {
            // if they're not within the tolerance of the next tile, then this is moot
            if (partyLeaderPosition.TileOffset.X < movementCollisionTolerance)
            {
                return true;
            }

            // if the player is at the outside left and right edges, 
            /* then check the diagonal tiles
            if (partyLeaderPosition.TileOffset.Y < -movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X + 1,
                    partyLeaderPosition.TilePosition.Y - 1)))
                {
                    return false;
                }
            }
            else if (partyLeaderPosition.TileOffset.Y > movementCollisionTolerance)
            {
                if (map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X + 1,
                    partyLeaderPosition.TilePosition.Y + 1)))
                {
                    return false;
                }
            }
            */

            foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
            {
                if (TileOverrideTrigger.mapName == Map.Name &&
                    TileOverrideTrigger.active)
                {
                    foreach (TileOverride over in TileOverrideTrigger.overrides)
                    {
                        if (over.position.X == partyLeaderPosition.TilePosition.X+1 &&
                            over.position.Y == partyLeaderPosition.TilePosition.Y &&
                            over.layer == 4)
                        {
                            if (over.newValue == 0)
                                return true;
                            else
                                return false;
                        }
                    }
                }
            }

            // check the tile to the right of the current one
            return !map.IsBlocked(new Point(
                    partyLeaderPosition.TilePosition.X + 1,
                    partyLeaderPosition.TilePosition.Y));
        }


        #endregion


        #region Updating


        /// <summary>
        /// Update the tile engine.
        /// </summary>
        public static void Update(GameTime gameTime)
        {
            // check for auto-movement
            Vector2 autoMovement = UpdatePartyLeaderAutoMovement(gameTime);

            // if there is no auto-movement, handle user controls
            userMovement = Vector2.Zero;
            if (autoMovement == Vector2.Zero)
            {
                userMovement = UpdateUserMovement(gameTime);
                // calculate the desired position
                if (userMovement != Vector2.Zero)
                {
                    Point desiredTilePosition = partyLeaderPosition.TilePosition;
                    Vector2 desiredTileOffset = partyLeaderPosition.TileOffset;
                    PlayerPosition.CalculateMovement(
                        Vector2.Multiply(userMovement, 15f),
                        ref desiredTilePosition, ref desiredTileOffset);
                    // check for collisions or encounters in the new tile
                    if ((partyLeaderPosition.TilePosition != desiredTilePosition) && 
                        !MoveIntoTile(desiredTilePosition))
                    {
                        userMovement = Vector2.Zero;
                    }
                     
                }
            }

            Vector2 playerPosition = PartyLeaderPosition.ScreenPosition - mapOriginPosition;

            //move blocks
            userMovement = updateBlocks(userMovement, playerPosition);


            //active or deactivate switches
            updateSwitches(playerPosition);

            //check switch TileOverrideTriggers
            updateTileOverrideTriggers();

            // move the party
            Point oldPartyLeaderTilePosition = partyLeaderPosition.TilePosition;

            //if(Session.holdButton == false)
            partyLeaderPosition.Move(autoMovement + userMovement, (Session.CurrentCutscene == null || Session.CurrentCutscene.allowInput));

            if(userMovement == Vector2.Zero)
                Session.holdButton = false;
            else
                Session.holdButton = true;

            // if the tile position has changed, check for random combat
            if ((autoMovement == Vector2.Zero) &&
                (partyLeaderPosition.TilePosition != oldPartyLeaderTilePosition))
            {
                Session.CheckForRandomCombat(Map.RandomCombat);
            }



            Vector2 oldmap = mapOriginPosition;

            // adjust the map origin so that the party is at the center of the viewport
            mapOriginPosition += viewportCenter - (partyLeaderPosition.ScreenPosition + 
                Session.Party.Players[0].MapSprite.SourceOffset);

            // make sure the boundaries of the map are never inside the viewport
            mapOriginPosition.X = MathHelper.Min(mapOriginPosition.X, viewport.X);
            mapOriginPosition.Y = MathHelper.Min(mapOriginPosition.Y, viewport.Y);
            mapOriginPosition.X += MathHelper.Max(
                (viewport.X + viewport.Width) - 
                (mapOriginPosition.X + map.MapDimensions.X * map.TileSize.X), 0f);
            mapOriginPosition.Y += MathHelper.Max(
                (viewport.Y + viewport.Height) - 
                (mapOriginPosition.Y + map.MapDimensions.Y * map.TileSize.Y), 0f);


            scrollMovement = mapOriginPosition - oldmap;


        }

        private static Vector2 updateBlocks(Vector2 userMovement, Vector2 playerPosition)
        {
            foreach (MapEntry<Block> block in map.BlockEntries)
            {
                Vector2 position = new Vector2(
                 block.Content.Position.X,
                 block.Content.Position.Y);

                Vector2 newPosition = block.Content.Position;

                //if player collides with block from left/right 
                if (playerPosition.X + userMovement.X < position.X + 64 &&
                    playerPosition.X + userMovement.X > position.X - 64 &&
                    playerPosition.Y < position.Y + 64 &&
                    playerPosition.Y > position.Y - 64)
                {

                    userMovement.X = 0;

                    //move block and player
                    float x = newPosition.X;

                    if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterLeft))
                    {

                        userMovement.X = -4;
                        x = block.Content.Position.X - 4;

                        if (block.Content.MoveType == BlockMoveType.Slide)
                            block.Content.SlideDirection = slideDirection.left;

                        //check if the bottom left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2) + userMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            userMovement.X = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2) + userMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            userMovement.X = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterRight))
                    {
                        userMovement.X = 4;
                        x = block.Content.Position.X + 4;

                        if (block.Content.MoveType == BlockMoveType.Slide)
                            block.Content.SlideDirection = slideDirection.right;

                        //check if the bottom right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2) + userMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            userMovement.X = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2) + userMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            userMovement.X = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    //check for block hitting other blocks
                    foreach (MapEntry<Block> otherBlock in map.BlockEntries)
                    {
                        //cancel player and block movement
                        if (block != otherBlock &&
                            block.Content.Position.X + userMovement.X < otherBlock.Content.Position.X + 64 &&
                            block.Content.Position.X + userMovement.X > otherBlock.Content.Position.X - 64 &&
                            block.Content.Position.Y < otherBlock.Content.Position.Y + 64 &&
                            block.Content.Position.Y > otherBlock.Content.Position.Y - 64)
                        {
                            userMovement.X = 0;
                            x = block.Content.Position.X;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    newPosition = new Vector2(x, newPosition.Y);
                }

                //if player collides with block from top/bottom
                if (playerPosition.Y + userMovement.Y < position.Y + 64 &&
                    playerPosition.Y + userMovement.Y > position.Y - 64 &&
                    playerPosition.X < position.X + 64 &&
                    playerPosition.X > position.X - 64)
                {
                    userMovement.Y = 0;

                    //move block and player
                    float y = newPosition.Y;

                    if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterUp))
                    {
                        userMovement.Y = -4;
                        y = block.Content.Position.Y - 4;

                        if (block.Content.MoveType == BlockMoveType.Slide)
                            block.Content.SlideDirection = slideDirection.up;

                        //check if the top left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2) + userMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            userMovement.Y = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2) + userMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            userMovement.Y = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterDown))
                    {
                        userMovement.Y = 4;
                        y = block.Content.Position.Y + 4;

                        if (block.Content.MoveType == BlockMoveType.Slide)
                            block.Content.SlideDirection = slideDirection.down;

                        //check if the bottom left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2) + userMovement.Y)  / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            userMovement.Y = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the bottom right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2) + userMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            userMovement.Y = 0;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }


                    //check for block hitting other blocks
                    foreach (MapEntry<Block> otherBlock in map.BlockEntries)
                    {
                        //cancel player and block movement
                        if (block != otherBlock &&
                            block.Content.Position.Y + userMovement.Y < otherBlock.Content.Position.Y + 64 &&
                            block.Content.Position.Y + userMovement.Y > otherBlock.Content.Position.Y - 64 &&
                            block.Content.Position.X < otherBlock.Content.Position.X + 64 &&
                            block.Content.Position.X > otherBlock.Content.Position.X - 64)
                        {
                            userMovement.Y = 0;
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }

                    }


                    newPosition = new Vector2(newPosition.X, y);

                }


                //slide blocks
                if (block.Content.SlideDirection != slideDirection.none)
                {
                    float x = block.Content.Position.X;
                    float y = block.Content.Position.Y;

                    Vector2 blockMovement = Vector2.Zero;

                    if (block.Content.SlideDirection == slideDirection.left)
                    {
                        blockMovement.X -= 4 * 2;
                        x -= 4 * 2;

                        //check if the bottom left side of the block is in a collision tile
                         if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2) + blockMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top left side of the block is in a collision tile
                         if (map.IsBlocked(new Point(
                             (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2) + blockMovement.X) / Map.TileSize.X),
                             (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                         {
                             x = block.Content.Position.X;
                             block.Content.SlideDirection = slideDirection.none;
                         }

                    }

                    if (block.Content.SlideDirection == slideDirection.right)
                    {
                        blockMovement.X += 4 * 2;
                        x += 4 * 2;

                        //check if the bottom right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2) + blockMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2) + blockMovement.X) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2)) / Map.TileSize.Y))))
                        {
                            x = block.Content.Position.X;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    if (block.Content.SlideDirection == slideDirection.up)
                    {
                        blockMovement.Y -= 4 * 2;
                        y -= 4 * 2;

                        //check if the top left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2) + blockMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the top right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y - (Map.TileSize.Y / 2) + blockMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    if (block.Content.SlideDirection == slideDirection.down)
                    {
                        blockMovement.Y += 4 * 2;
                        y += 4 * 2;

                        //check if the bottom left side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X - (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2) + blockMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                        //check if the bottom right side of the block is in a collision tile
                        if (map.IsBlocked(new Point(
                            (int)Math.Round((block.Content.Position.X + (Map.TileSize.X / 2)) / Map.TileSize.X),
                            (int)Math.Round((block.Content.Position.Y + (Map.TileSize.Y / 2) + blockMovement.Y) / Map.TileSize.Y))))
                        {
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    //check for block hitting other blocks
                    foreach (MapEntry<Block> otherBlock in map.BlockEntries)
                    {
                        //cancel player and block movement
                        if (block != otherBlock &&
                            block.Content.Position.Y + blockMovement.Y < otherBlock.Content.Position.Y + 64 &&
                            block.Content.Position.Y + blockMovement.Y > otherBlock.Content.Position.Y - 64 &&
                            block.Content.Position.X + blockMovement.X < otherBlock.Content.Position.X + 64 &&
                            block.Content.Position.X + blockMovement.X > otherBlock.Content.Position.X - 64)
                        {
                            x = block.Content.Position.X;
                            y = block.Content.Position.Y;
                            block.Content.SlideDirection = slideDirection.none;
                        }
                    }

                    newPosition = new Vector2(x, y);
                }

                block.Content.Position = newPosition;
            }

            return userMovement;
        }


        private static void updateSwitches(Vector2 playerPosition)
        {
            foreach(MapEntry<Switch> Switch in map.SwitchEntries)
            {
                bool collisionDetected = false;

                foreach(MapEntry<Block> block in map.BlockEntries)
                {
                    if (block.Content.Position.Y < Switch.Content.Position.Y + 64 &&
                        block.Content.Position.Y > Switch.Content.Position.Y - 64 &&
                        block.Content.Position.X < Switch.Content.Position.X + 64 &&
                        block.Content.Position.X > Switch.Content.Position.X - 64)
                    {
                        collisionDetected = true;
                    }
                }


                if (playerPosition.X < Switch.Content.Position.X + 64 &&
                    playerPosition.X > Switch.Content.Position.X - 64 &&
                    playerPosition.Y < Switch.Content.Position.Y + 64 &&
                    playerPosition.Y > Switch.Content.Position.Y - 64)
                {
                    collisionDetected = true;
                }

                if (collisionDetected)
                    Switch.Content.Active = true;
                else
                {
                    if (!Switch.Content.AlwaysActive)
                    {
                        Switch.Content.Active = false;
                    }
                }

            }
        }

        private static void updateTileOverrideTriggers()
        {
            foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
            {
                /*
                bool allActive = false;

                foreach (MapEntry<RolePlayingGameData.Switch> Switch in TileOverrideTrigger.switchCheck)
                {
                    if (!Switch.Content.Active)
                        allActive = false;
                }

                if (allActive)
                    TileOverrideTrigger.active = true;
                else if (!TileOverrideTrigger.alwaysActive)
                    TileOverrideTrigger.active = false;
                 */
            }
        }

        /// <summary>
        /// Performs any actions associated with moving into a new tile.
        /// </summary>
        /// <returns>True if the character can move into the tile.</returns>
        private static bool MoveIntoTile(Point mapPosition)
        {
            /* if the tile is blocked, then this is simple
            if (map.IsBlocked(mapPosition))
            {
                return false;
            }
            */
            // check for anything that might be in the tile
            if (Session.EncounterTile(mapPosition))
            {
                return false;
            }
            // nothing stops the party from moving into the tile
            return true;
        }


        #endregion


        #region Drawing


        /// <summary>
        /// Draw the visible tiles in the given map layers.
        /// </summary>
        public static void DrawLayers(SpriteBatch spriteBatch, bool drawBase, 
            bool drawFringe, bool drawObject)
        {
            // check the parameters
            if (spriteBatch == null)
            {
                throw new ArgumentNullException("spriteBatch");
            }
            if (!drawBase && !drawFringe && !drawObject)
            {
                return;
            }

            Rectangle destinationRectangle =  
                new Rectangle(0, 0, map.TileSize.X, map.TileSize.Y);

            for (int y = 0; y < map.MapDimensions.Y; y++)
            {
                for (int x = 0; x < map.MapDimensions.X; x++)
                {
                    destinationRectangle.X = 
                        (int)mapOriginPosition.X + x * map.TileSize.X;
                    destinationRectangle.Y = 
                        (int)mapOriginPosition.Y + y * map.TileSize.Y;

                    //destinationRectangle.X -= destinationRectangle.X % 2;
                    //destinationRectangle.Y -= destinationRectangle.Y % 2;


                    bool overrideSkip = false;

                    foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
                    {
                        if (TileOverrideTrigger.mapName == Map.Name && 
                            TileOverrideTrigger.active)
                        {
                            foreach (TileOverride over in TileOverrideTrigger.overrides)
                            {
                                if (over.position.X == x &&
                                    over.position.Y == y)
                                {
                                    if(over.layer == 3 && drawFringe)
                                        overrideSkip = true;

                                    if ((over.layer == 1 || over.layer == 2) && (drawFringe && drawBase))
                                        overrideSkip = true;
                                }
                            }
                        }
                    }


                    // If the tile is inside the screen
                    if (CheckVisibility(destinationRectangle))
                    {
                        Point mapPosition = new Point(x, y);
                        if (drawBase)
                        {
                            Rectangle sourceRectangle =
                                map.GetBaseLayerSourceRectangleMinusOne(mapPosition);
                            if (sourceRectangle != Rectangle.Empty || overrideSkip)
                            {

                                foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
                                {
                                    if (TileOverrideTrigger.mapName == Map.Name && 
                                        TileOverrideTrigger.active)
                                    {
                                        foreach (TileOverride over in TileOverrideTrigger.overrides)
                                        {
                                            if (over.position.X == x &&
                                                over.position.Y == y &&
                                                over.layer == 1)
                                            {
                                                if (over.newValue == -1)
                                                    sourceRectangle = Rectangle.Empty;
                                                else
                                                    sourceRectangle = 
                                                        new Rectangle(
                                                        (over.newValue % Map.TilesPerRow) * Map.TileSize.X,
                                                        (over.newValue / Map.TilesPerRow) * Map.TileSize.Y,
                                                        Map.TileSize.X, Map.TileSize.Y);
                                            }
                                        }
                                    }
                                }

                                spriteBatch.Draw(map.Texture, destinationRectangle,
                                    sourceRectangle, Color.White);
                            }
                        }
                        if (drawFringe)
                        {
                            Rectangle sourceRectangle =
                                map.GetFringeLayerSourceRectangleMinusOne(mapPosition);
                            if (sourceRectangle != Rectangle.Empty || overrideSkip)
                            {
                                foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
                                {
                                    if (TileOverrideTrigger.mapName == Map.Name &&
                                        TileOverrideTrigger.active)
                                    {
                                        foreach (TileOverride over in TileOverrideTrigger.overrides)
                                        {
                                            if (over.position.X == x &&
                                                over.position.Y == y &&
                                                over.layer == 2)
                                            {
                                                if (over.newValue == -1)
                                                    sourceRectangle = Rectangle.Empty;
                                                else
                                                    sourceRectangle =
                                                        new Rectangle(
                                                        (over.newValue % Map.TilesPerRow) * Map.TileSize.X,
                                                        (over.newValue / Map.TilesPerRow) * Map.TileSize.Y,
                                                        Map.TileSize.X, Map.TileSize.Y);
                                            }
                                        }
                                    }
                                }

                                spriteBatch.Draw(map.Texture, destinationRectangle,
                                    sourceRectangle, Color.White);
                            } 
                        }
                        if (drawObject)
                        {
                            Rectangle sourceRectangle =
                                map.GetObjectLayerSourceRectangleMinusOne(mapPosition);
                            if (sourceRectangle != Rectangle.Empty || overrideSkip)
                            {
                                foreach (TileOverrideTrigger TileOverrideTrigger in Session.TileOverrideTriggers)
                                {
                                    if (TileOverrideTrigger.mapName == Map.Name &&
                                        TileOverrideTrigger.active)
                                    {
                                        foreach (TileOverride over in TileOverrideTrigger.overrides)
                                        {
                                            if (over.position.X == x &&
                                                over.position.Y == y &&
                                                over.layer == 3)
                                            {
                                                if (over.newValue == -1)
                                                    sourceRectangle = Rectangle.Empty;
                                                else
                                                    sourceRectangle =
                                                        new Rectangle(
                                                        (over.newValue % Map.TilesPerRow) * Map.TileSize.X,
                                                        (over.newValue / Map.TilesPerRow) * Map.TileSize.Y,
                                                        Map.TileSize.X, Map.TileSize.Y);
                                            }
                                        }
                                    }
                                }

                                spriteBatch.Draw(map.Texture, destinationRectangle,
                                    sourceRectangle, Color.White);
                            } 
                        }
                    }
                }
            } 

        }


        public static void PrintMap(SpriteBatch spriteBatch, bool drawBase,
            bool drawFringe, bool drawObject, Texture2D printTex)
        {
            Texture2D t2d = new Texture2D(spriteBatch.GraphicsDevice, map.Texture.Width, map.Texture.Height, false, spriteBatch.GraphicsDevice.PresentationParameters.BackBufferFormat);

            
            // check the parameters
            if (spriteBatch == null)
            {
                throw new ArgumentNullException("spriteBatch");
            }
            if (!drawBase && !drawFringe && !drawObject)
            {
                return;
            }

            Rectangle destinationRectangle =
                new Rectangle(0, 0, map.TileSize.X/2, map.TileSize.Y/2);

            for (int y = 0; y < map.MapDimensions.Y; y++)
            {
                for (int x = 0; x < map.MapDimensions.X; x++)
                {
                    destinationRectangle.X = x * map.TileSize.X/2;
                    destinationRectangle.Y = y * map.TileSize.Y/2;


                    if (true)
                    {
                        Point mapPosition = new Point(x, y);
                        if (drawBase)
                        {
                            int baseLayerValue = map.GetBaseLayerValue(mapPosition) - 1;

                            Rectangle sourceRectangle =  new Rectangle(
                                (baseLayerValue % map.TilesPerRow) * map.TileSize.X/2,
                                (baseLayerValue / map.TilesPerRow) * map.TileSize.Y/2,
                                 map.TileSize.X / 2, map.TileSize.Y / 2);


                            if (sourceRectangle != Rectangle.Empty)
                            {
                                if ((x > 0 && x < 64) && (y > 0 && y < 64))
                                {
                                    byte[] tileData = new byte[sourceRectangle.Width * sourceRectangle.Height * 4];

                                    printTex.GetData(0, sourceRectangle, tileData, 0, tileData.Length);

                                    t2d.SetData<byte>(0, destinationRectangle, tileData, 0, tileData.Length);

                                }
                            }
                        }

                        if (drawFringe)
                        {
                            int fringeLayerValue = map.GetFringeLayerValue(mapPosition) - 1;

                            Rectangle sourceRectangle = new Rectangle(
                                (fringeLayerValue % map.TilesPerRow) * map.TileSize.X / 2,
                                (fringeLayerValue / map.TilesPerRow) * map.TileSize.Y / 2,
                                 map.TileSize.X / 2, map.TileSize.Y / 2);


                            if (sourceRectangle != Rectangle.Empty && fringeLayerValue >= 0)
                            {
                                if ((x > 0 && x < 64) && (y > 0 && y < 64))
                                {
                                    byte[] tileData = new byte[sourceRectangle.Width * sourceRectangle.Height * 4];

                                    printTex.GetData(0, sourceRectangle, tileData, 0, tileData.Length);

                                    t2d.SetData<byte>(0, destinationRectangle, tileData, 0, tileData.Length);

                                }
                            }
                        }

                        if (drawObject)
                        {
                            int objectLayerValue = map.GetObjectLayerValue(mapPosition) - 1;

                            Rectangle sourceRectangle = new Rectangle(
                                (objectLayerValue % map.TilesPerRow) * map.TileSize.X / 2,
                                (objectLayerValue / map.TilesPerRow) * map.TileSize.Y / 2,
                                 map.TileSize.X / 2, map.TileSize.Y / 2);


                            if (sourceRectangle != Rectangle.Empty && objectLayerValue >= 0)
                            {
                                if ((x > 0 && x < 64) && (y > 0 && y < 64))
                                {
                                    byte[] tileData = new byte[sourceRectangle.Width * sourceRectangle.Height * 4];

                                    printTex.GetData(0, sourceRectangle, tileData, 0, tileData.Length);

                                    t2d.SetData<byte>(0, destinationRectangle, tileData, 0, tileData.Length);

                                }
                            }
                        }
                    }
                }
            }


            int i = 0;
            string name = "ScreenShot" + i.ToString() + ".png";
            while (File.Exists(name))
            {
                i += 1;
                name = "ScreenShot" + i.ToString() + ".png";

            }

            Stream st = new FileStream(name, FileMode.Create);

            t2d.SaveAsPng(st, t2d.Width, t2d.Height);

            st.Close();

            t2d.Dispose();


        }



        /// <summary>
        /// Returns true if the given rectangle is within the viewport.
        /// </summary>
        public static bool CheckVisibility(Rectangle screenRectangle)
        {
            return ((screenRectangle.X > viewport.X - screenRectangle.Width) &&
                (screenRectangle.Y > viewport.Y - screenRectangle.Height) &&
                (screenRectangle.X < viewport.X + viewport.Width) &&
                (screenRectangle.Y < viewport.Y + viewport.Height));
        }

            
        #endregion
    }
}
