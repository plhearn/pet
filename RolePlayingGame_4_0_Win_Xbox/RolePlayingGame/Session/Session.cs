#region File Description
//-----------------------------------------------------------------------------
// Session.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;
using RolePlayingGameData;
#endregion

namespace RolePlaying
{
    class Session
    {
        #region Singleton


        /// <summary>
        /// The single Session instance that can be active at a time.
        /// </summary>
        private static Session singleton;


        #endregion

        public static bool holdButton = false;
        const int MAP_TRANSITION_FADE_TIME = 60;
        const float MAP_TRANSITION_TRAVEL_DISTANCE = 15f;

        #region Party

        /// <summary>
        /// The party that is playing the game right now.
        /// </summary>
        private Party party;

        /// <summary>
        /// The party that is playing the game right now.
        /// </summary>
        public static Party Party
        {
            get { return (singleton == null ? null : singleton.party); }
        }

        Vector2 oldCamPos;
        Vector2 playerProxyPosition;
        Vector2 playerProxyMovement;
        Vector2 playerProxyStartPosition;
        Vector2 playerProxyAutoMove;

        public List<QuestNpc> npcs;
        public static List<Vector2> npcPositions;
        public List<Vector2> npcOldPositions;

        bool fadeTransition = false;
        Vector2 transitionMovement;
        MapEntry<Portal> transitionPortal;

        List<MapOverlay> garbageOverlays;


        public List<Cutscene> cutscenes = new List<Cutscene>();
        public Cutscene currentCutscene;

        public static Cutscene CurrentCutscene
        {
            get { return singleton.currentCutscene; }
        }


        public static List<TileOverrideTrigger> TileOverrideTriggers = new List<TileOverrideTrigger>();

        public static List<CutsceneTrigger> CutsceneTriggers = new List<CutsceneTrigger>();



        #endregion

        #region Map Effects

        public static List<Raindrop> raindrops = new List<Raindrop>();

        public static List<MapOverlay> mapOverlays = new List<MapOverlay>();


        #endregion


        #region Map

        /// <summary>
        /// Change the current map, arriving at the given portal if any.
        /// </summary>
        /// <param name="contentName">The asset name of the new map.</param>
        /// <param name="originalPortal">The portal from the previous map.</param>
        public static void ChangeMap(string contentName, Portal originalPortal)
        {


            TileEngine.autoPartyLeaderMovement = Vector2.Multiply(singleton.transitionMovement, MAP_TRANSITION_TRAVEL_DISTANCE);

            // make sure the content name is valid
            string mapContentName = contentName;
            if (!mapContentName.StartsWith(@"Maps\"))
            {
                mapContentName = Path.Combine(@"Maps", mapContentName);
            }

            // check for trivial movement - typically intra-map portals
            if ((TileEngine.Map != null) && (TileEngine.Map.AssetName == mapContentName))
            {
                TileEngine.SetMap(TileEngine.Map, originalPortal == null ? null :
                    TileEngine.Map.FindPortal(originalPortal.DestinationMapPortalName));
            }

            // load the map
            ContentManager content = singleton.screenManager.Game.Content;
            Map map = content.Load<Map>(mapContentName).Clone() as Map;

            // modify the map based on the world changes (removed chests, etc.).
            singleton.ModifyMap(map);

            //set block positions
            foreach (MapEntry<Block> block in map.BlockEntries)
            {
                block.Content.Position = new Vector2(block.MapPosition.X * map.TileSize.X, block.MapPosition.Y * map.TileSize.Y);
            }

            //set Switch positions
            foreach (MapEntry<RolePlayingGameData.Switch> Switch in map.SwitchEntries)
            {
                Switch.Content.Position = new Vector2(Switch.MapPosition.X * map.TileSize.X, Switch.MapPosition.Y * map.TileSize.Y);
            }


            // start playing the music for the new map
            //AudioManager.PlayMusic(map.MusicCueName);

            // set the new map into the tile engine
            TileEngine.SetMap(map, originalPortal == null ? null :
                map.FindPortal(originalPortal.DestinationMapPortalName));


            SetNPCPositions();


            //check for cutscene
            foreach (CutsceneTrigger trigger in CutsceneTriggers)
                if (trigger.mapName == (TileEngine.Map.Name + "START") && trigger.activated == false)
                        {
                            singleton.currentCutscene = trigger.cutscene;
                            
                            if(!trigger.repeat)
                                trigger.activated = true;
                        }


            /*
            if (TileEngine.Map.Effect == "rain")
            {
                loadRain();
            }
            else
            {
                //raindrops.Clear();
            }

            //mapOverlays.Clear();

            if (TileEngine.Map.Effect == "fog")
            {
                loadFog();
             * loadMist();
            }
            */

            removeOverlay("black");
            loadFade(MAP_TRANSITION_FADE_TIME, 1, 0);
        }



        public static void loadFade(int duration, float startOpacity, float endOpacity)
        {
            Texture2D fadeTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\GameScreens\FadeScreen");

            MapOverlay fade = new FadeOverlay(duration, startOpacity, endOpacity, fadeTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            fade.name = "fade";
            mapOverlays.Add(fade);
        }

        public static void loadColorFade(int duration, float startOpacity, float endOpacity, string color)
        {
            Texture2D fadeTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\" + color);

            MapOverlay fade = new FadeOverlay(duration, startOpacity, endOpacity, fadeTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            fade.name = "colorfade";
            mapOverlays.Add(fade);
        }

        public static void loadRain()
        {
            raindrops = new List<Raindrop>();

            Raindrop rain;

            Texture2D rainTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\rain64");

            for (int i = 0; i < 200; i++)
            {
                rain = new Raindrop();
                rain.TextureName = "rain";
                rain.FramesPerRow = rain.width;
                rain.FrameDimensions = new Point(rain.width, rain.width * 2);
                rain.AddAnimation(new Animation("rain", 1, 4, 1, false));
                rain.Texture = rainTex;
                rain.position = new Vector2(ScreenManager.GraphicsDevice.Viewport.Width / 4 + random.Next(ScreenManager.GraphicsDevice.Viewport.Width), -(rain.width * 2) - random.Next(ScreenManager.GraphicsDevice.Viewport.Height));
                rain.lifeTimer = random.Next(100);
                raindrops.Add(rain);
            }

            loadSong("rain");
        }

        public static void loadFog()
        {
            Texture2D fogTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\clouda");

            float speed = 0.3f;

            Vector2 drift = new Vector2(speed, speed);
            float opacity = 0.2f;

            MapOverlay fog = new MapOverlay(drift, opacity, fogTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);

            mapOverlays.Add(fog);

            drift = new Vector2(speed, 0.0f);
            opacity = 0.2f;
            MapOverlay fog2 = new MapOverlay(drift, opacity, fogTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);

            mapOverlays.Add(fog2);

            drift = new Vector2(-speed, 0.0f);
            opacity = 0.2f;
            MapOverlay fog3 = new MapOverlay(drift, opacity, fogTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);

            mapOverlays.Add(fog3);

            drift = new Vector2(-speed, -speed);
            opacity = 0.2f;
            MapOverlay fog4 = new MapOverlay(drift, opacity, fogTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);

            fog.name = "fog";
            mapOverlays.Add(fog4);
        }


        public static void loadMist()
        {
            Texture2D mistTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\mist");

            float speed = 0.2f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 0.2f;

            MapOverlay mist = new MapOverlay(drift, opacity, mistTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            mist.name = "mist";
            mapOverlays.Add(mist);
        }


        public static void loadDarken()
        {
            Texture2D darkTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\GameScreens\FadeScreen");

            float speed = 0.0f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 0.6f;

            MapOverlay darken = new FadeInOverlay(drift, opacity, darkTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            darken.name = "darken";
            mapOverlays.Add(darken);
        }

        public static void loadBlack()
        {
            Texture2D darkTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\GameScreens\FadeScreen");

            float speed = 0.0f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 1f;

            MapOverlay black = new MapOverlay(drift, opacity, darkTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            black.name = "black";
            mapOverlays.Add(black);
        }

        public static void loadDusk()
        {
            Texture2D duskTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\GameScreens\FadeScreen");

            float speed = 0.0f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 0.6f;

            MapOverlay dusk = new MapOverlay(drift, opacity, duskTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            dusk.name = "darken";
            mapOverlays.Add(dusk);
        }


        public static void loadBlue()
        {
            Texture2D blueTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\blue");

            float speed = 0.0f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 0.3f;

            MapOverlay blue = new MapOverlay(drift, opacity, blueTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height);
            blue.name = "blue";
            mapOverlays.Add(blue);
        }


        public static void loadLightening()
        {
            Texture2D whiteTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\GameScreens\WhiteScreen");

            float speed = 0.0f;

            Vector2 drift = new Vector2(speed, 0);
            float opacity = 0.8f;

            SoundEffect thunder = singleton.screenManager.Game.Content.Load<SoundEffect>(@"Audio\thunder");

            LightFlashOverlay white = new LightFlashOverlay(drift, opacity, whiteTex, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height, thunder);

            mapOverlays.Add(white);


        }


        public static void loadSong(string song)
        {
            Song music;

            if (song != "")
            {
                music = singleton.screenManager.Game.Content.Load<Song>(@"Audio\" + song);
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(music);
                //MediaPlayer.Volume = 0.1f;
            }
            else
                MediaPlayer.Stop();
        }

        public static void loadSoundEffect(string name)
        {
            SoundEffect sound = singleton.screenManager.Game.Content.Load<SoundEffect>(@"Audio\" + name);

            sound.Play();
        }


        public static void removeOverlay(string str)
        {
            string[] removeList = str.Split('_');

            bool clear = false;

            singleton.garbageOverlays = new List<MapOverlay>();

            foreach (MapOverlay overlay in mapOverlays)
            {
                foreach (string item in removeList)
                {
                    if (item == overlay.name)
                        singleton.garbageOverlays.Add(overlay);

                    if (item == "rain")
                        raindrops.Clear();

                    if (item == "clear")
                        clear = true;
                }
            }

            foreach (MapOverlay overlay in singleton.garbageOverlays)
                mapOverlays.Remove(overlay);

            singleton.garbageOverlays.Clear();

            if (clear)
                mapOverlays.Clear();
        }

        public static void SetNPCPositions()
        {
            singleton.npcs = new List<QuestNpc>();
            npcPositions = new List<Vector2>();
            singleton.npcOldPositions = new List<Vector2>();

            foreach (MapEntry<QuestNpc> questNpcEntry in TileEngine.Map.QuestNpcEntries)
            {
                if (questNpcEntry.Content == null)
                {
                    continue;
                }

                Vector2 position = new Vector2(questNpcEntry.Content.MapPosition.X, questNpcEntry.Content.MapPosition.Y);

                singleton.npcs.Add(questNpcEntry.Content);
                npcPositions.Add(position);
                singleton.npcOldPositions.Add(Vector2.Zero);
            }
        }



        public static void moveToTile(string tileStr)
        {
            string[] tile = tileStr.Split('_');

            Vector2 startPos = TileEngine.PartyLeaderPosition.ScreenPosition;
            Vector2 endPos = TileEngine.GetScreenPosition(new Point(int.Parse(tile[0]), int.Parse(tile[1])));

            singleton.playerProxyAutoMove = endPos - startPos;
        }

        public static void setCamera(string tileStr)
        {
            string[] tile = tileStr.Split(',');

            TileEngine.PartyLeaderPosition.TilePosition = new Point(int.Parse(tile[0]), int.Parse(tile[1]));
            TileEngine.PartyLeaderPosition.TileOffset = new Vector2(float.Parse(tile[2]), float.Parse(tile[3]));
        }

        public static Direction facePlayer(Vector2 npcPosition)
        {
            Vector2 playerPosition = new Vector2(
                TileEngine.PartyLeaderPosition.TilePosition.X * TileEngine.Map.TileSize.X + TileEngine.PartyLeaderPosition.TileOffset.X
               ,TileEngine.PartyLeaderPosition.TilePosition.Y * TileEngine.Map.TileSize.Y + TileEngine.PartyLeaderPosition.TileOffset.Y
               );

            float deltaX = npcPosition.X - playerPosition.X;
            float deltaY = npcPosition.Y - playerPosition.Y;

            float degrees = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

            Direction dir = Direction.North;

            if (degrees >= 45 && degrees < 135)
                dir = Direction.North;

            if (degrees <= -45 && degrees > -135)
                dir = Direction.South;

            if (degrees < 45 || degrees > -45)
                dir = Direction.West;

            if (degrees > 135 || degrees < -135)
                dir = Direction.East;

            return dir;
        }
        
        /// <summary>
        /// Perform any actions associated withe the given tile.
        /// </summary>
        /// <param name="mapPosition">The tile to check.</param>
        /// <returns>True if anything was encountered, false otherwise.</returns>
        public static bool EncounterTile(Point mapPosition)
        {
            //check for cutscene
            foreach(CutsceneTrigger trigger in CutsceneTriggers)
                if(trigger.mapName == TileEngine.Map.Name && trigger.activated == false)
                    foreach (Point p in trigger.tiles)
                        if (p == mapPosition)
                        {
                            singleton.currentCutscene = trigger.cutscene;

                            if (!trigger.repeat)
                                trigger.activated = true;
                        }


            // look for fixed-combats from the quest
            if ((singleton.quest != null) &&
                ((singleton.quest.Stage == Quest.QuestStage.InProgress) ||
                 (singleton.quest.Stage == Quest.QuestStage.RequirementsMet)))
            {
                MapEntry<FixedCombat> fixedCombatEntry =
                    singleton.quest.FixedCombatEntries.Find(
                        delegate(WorldEntry<FixedCombat> worldEntry)
                        {
                            return 
                                TileEngine.Map.AssetName.EndsWith(
                                    worldEntry.MapContentName) && 
                                worldEntry.MapPosition == mapPosition;
                        });
                if (fixedCombatEntry != null)
                {
                    if((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                        Session.EncounterFixedCombat(fixedCombatEntry);

                    return true;
                }
            }

            // look for fixed-combats from the map
            MapEntry<FixedCombat> fixedCombatMapEntry = 
                TileEngine.Map.FixedCombatEntries.Find(
                    delegate(MapEntry<FixedCombat> mapEntry)
                    {
                        return mapEntry.MapPosition == mapPosition;
                    });
            if (fixedCombatMapEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    Session.EncounterFixedCombat(fixedCombatMapEntry);
                
                return true;
            }

            // look for chests from the current quest
            if (singleton.quest != null)
            {
                MapEntry<Chest> chestEntry = singleton.quest.ChestEntries.Find(
                    delegate(WorldEntry<Chest> worldEntry)
                    {
                        return
                            TileEngine.Map.AssetName.EndsWith(
                                worldEntry.MapContentName) &&
                            worldEntry.MapPosition == mapPosition;
                    });
                if (chestEntry != null)
                {
                    if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                        Session.EncounterChest(chestEntry);

                    return true;
                }
            }

            // look for chests from the map
            MapEntry<Chest> chestMapEntry =
                TileEngine.Map.ChestEntries.Find(delegate(MapEntry<Chest> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (chestMapEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                     Session.EncounterChest(chestMapEntry);

                return true;
            }

            // look for player NPCs from the map
            MapEntry<Player> playerNpcEntry =
                TileEngine.Map.PlayerNpcEntries.Find(delegate(MapEntry<Player> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (playerNpcEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    Session.EncounterPlayerNpc(playerNpcEntry);

                return true;
            }

            // look for quest NPCs from the map
            MapEntry<QuestNpc> questNpcEntry =
                TileEngine.Map.QuestNpcEntries.Find(delegate(MapEntry<QuestNpc> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (questNpcEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    Session.EncounterQuestNpc(questNpcEntry);

                return true;
            }

            // look for portals from the map
            MapEntry<Portal> portalEntry =
                TileEngine.Map.PortalEntries.Find(delegate(MapEntry<Portal> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (portalEntry != null)
            {
                Session.EncounterPortal(portalEntry);
                //TileEngine.autoPartyLeaderMovement = Vector2.Zero;
                return true;
            }

            // look for inns from the map
            MapEntry<Inn> innEntry =
                TileEngine.Map.InnEntries.Find(delegate(MapEntry<Inn> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (innEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    Session.EncounterInn(innEntry);

                return true;
            }

            // look for stores from the map
            MapEntry<Store> storeEntry =
                TileEngine.Map.StoreEntries.Find(delegate(MapEntry<Store> mapEntry)
                {
                    return mapEntry.MapPosition == mapPosition;
                });
            if (storeEntry != null)
            {
                if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    Session.EncounterStore(storeEntry);

                return true;
            }

            // nothing encountered
            return false;
        }


        /// <summary>
        /// Performs the actions associated with encountering a FixedCombat entry.
        /// </summary>
        public static void EncounterFixedCombat(MapEntry<FixedCombat> fixedCombatEntry)
        {
            // check the parameter
            if ((fixedCombatEntry == null) || (fixedCombatEntry.Content == null))
            {
                throw new ArgumentNullException("fixedCombatEntry");
            }

            if (!CombatEngine.IsActive)
            {
                // start combat
                CombatEngine.StartNewCombat(fixedCombatEntry);
            }
        }


        /// <summary>
        /// Performs the actions associated with encountering a Chest entry.
        /// </summary>
        public static void EncounterChest(MapEntry<Chest> chestEntry)
        {
            // check the parameter
            if ((chestEntry == null) || (chestEntry.Content == null))
            {
                throw new ArgumentNullException("chestEntry");
            }

            // add the chest screen
            singleton.screenManager.AddScreen(new ChestScreen(chestEntry));
        }


        /// <summary>
        /// Performs the actions associated with encountering a player-NPC entry.
        /// </summary>
        public static void EncounterPlayerNpc(MapEntry<Player> playerEntry)
        {
            // check the parameter
            if ((playerEntry == null) || (playerEntry.Content == null))
            {
                throw new ArgumentNullException("playerEntry");
            }

            // add the player-NPC screen
            singleton.screenManager.AddScreen(new PlayerNpcScreen(playerEntry));
        }


        /// <summary>
        /// Performs the actions associated with encountering a QuestNpc entry.
        /// </summary>
        public static void EncounterQuestNpc(MapEntry<QuestNpc> questNpcEntry)
        {
            // check the parameter
            if ((questNpcEntry == null) || (questNpcEntry.Content == null))
            {
                throw new ArgumentNullException("questNpcEntry");
            }

            // add the quest-NPC screen
            //singleton.screenManager.AddScreen(new QuestNpcScreen(questNpcEntry));

            foreach (Cutscene cutscene in singleton.cutscenes)
            {
                if (cutscene.name == questNpcEntry.Content.Name)
                    singleton.currentCutscene = cutscene;
            }
        }


        /// <summary>
        /// Performs the actions associated with encountering an Inn entry.
        /// </summary>
        public static void EncounterInn(MapEntry<Inn> innEntry)
        {
            // check the parameter
            if ((innEntry == null) || (innEntry.Content == null))
            {
                throw new ArgumentNullException("innEntry");
            }

            // add the inn screen
            singleton.screenManager.AddScreen(new InnScreen(innEntry.Content));
        }


        /// <summary>
        /// Performs the actions associated with encountering a Store entry.
        /// </summary>
        public static void EncounterStore(MapEntry<Store> storeEntry)
        {
            // check the parameter
            if ((storeEntry == null) || (storeEntry.Content == null))
            {
                throw new ArgumentNullException("storeEntry");
            }

            // add the store screen
            singleton.screenManager.AddScreen(new StoreScreen(storeEntry.Content));
        }
        

        /// <summary>
        /// Performs the actions associated with encountering a Portal entry.
        /// </summary>
        public static void EncounterPortal(MapEntry<Portal> portalEntry)
        {
            // check the parameter

            if (portalEntry == null)
            {

            }
            else if ((portalEntry.Content == null))
            {
                throw new ArgumentNullException("portalEntry");
            }

            if (singleton.fadeTransition == false)
            {
                singleton.fadeTransition = true;
                singleton.transitionMovement = TileEngine.userMovement;
                singleton.transitionPortal = portalEntry;
                loadFade(MAP_TRANSITION_FADE_TIME, 0, 1);
            }

            bool fadeFound = false;

            foreach (MapOverlay overlay in mapOverlays)
                if (overlay.name == "fade")
                    fadeFound = true;

            if (singleton.fadeTransition)
            {
                if (fadeFound)
                {
                    TileEngine.autoPartyLeaderMovement = singleton.transitionMovement;
                }
                else
                {
                    singleton.fadeTransition = false;
                    // change to the new map
                    ChangeMap(singleton.transitionPortal.Content.DestinationMapContentName,
                        singleton.transitionPortal.Content);
                }
            }
        }


        /// <summary>
        /// Check if a random combat should start.  If so, start combat immediately.
        /// </summary>
        /// <returns>True if combat was started, false otherwise.</returns>
        public static bool CheckForRandomCombat(RandomCombat randomCombat)
        {
            // check the parameter
            if ((randomCombat == null) || (randomCombat.CombatProbability <= 0))
            {
                return false;
            }

            // check to see if combat has already started
            if (CombatEngine.IsActive)
            {
                return false;
            }

            // check to see if the random combat starts
            int randomCombatCheck = random.Next(100);
            if (randomCombatCheck < randomCombat.CombatProbability)
            {
                // start combat immediately
                CombatEngine.StartNewCombat(randomCombat);
                return true;
            }

            // combat not started
            return false;
        }


        #endregion
        

        #region Quests


        /// <summary>
        /// The main quest line for this session.
        /// </summary>
        private QuestLine questLine;

        /// <summary>
        /// The main quest line for this session.
        /// </summary>
        public static QuestLine QuestLine
        {
            get { return (singleton == null ? null : singleton.questLine); }
        }


        /// <summary>
        /// If true, the main quest line for this session is complete.
        /// </summary>
        public static bool IsQuestLineComplete
        {
            get
            {
                if ((singleton == null) || (singleton.questLine == null) ||
                    (singleton.questLine.QuestContentNames == null))
                {
                    return false;
                }
                return singleton.currentQuestIndex >= 
                    singleton.questLine.QuestContentNames.Count;
            }
        }


        /// <summary>
        /// The current quest in this session.
        /// </summary>
        private Quest quest;

        /// <summary>
        /// The current quest in this session.
        /// </summary>
        public static Quest Quest
        {
            get { return (singleton == null ? null : singleton.quest); }
        }


        /// <summary>
        /// The index of the current quest into the quest line.
        /// </summary>
        private int currentQuestIndex = 0;

        /// <summary>
        /// The index of the current quest into the quest line.
        /// </summary>
        public static int CurrentQuestIndex
        {
            get { return (singleton == null ? -1 : singleton.currentQuestIndex); }
        }


        /// <summary>
        /// Update the current quest and quest line for this session.
        /// </summary>
        public void UpdateQuest()
        {
            // check the singleton's state to see if we should care about quests
            if ((party == null) || (questLine == null))
            {
                return;
            }

            // if we don't have a quest, then take the next one from teh list
            if ((quest == null) && (questLine.Quests.Count > 0) && 
                !Session.IsQuestLineComplete)
            {
                quest = questLine.Quests[currentQuestIndex];
                quest.Stage = Quest.QuestStage.NotStarted;
                // clear the monster-kill record
                party.MonsterKills.Clear();
                // clear the modified-quest lists
                modifiedQuestChests.Clear();
                removedQuestChests.Clear();
                removedQuestFixedCombats.Clear();
            }

            // handle quest-stage transitions
            if ((quest != null) && !Session.IsQuestLineComplete)
            {
                switch (quest.Stage)
                {
                    case Quest.QuestStage.NotStarted:
                        // start the new quest
                        quest.Stage = Quest.QuestStage.InProgress;
                        if (!quest.AreRequirementsMet)
                        {
                            // show the announcement of the quest and the requirements
                            //ScreenManager.AddScreen(new QuestLogScreen(quest));
                        }
                        break;

                    case Quest.QuestStage.InProgress:
                        // update monster requirements
                        foreach (QuestRequirement<Monster> monsterRequirement in
                            quest.MonsterRequirements)
                        {
                            monsterRequirement.CompletedCount = 0;
                            Monster monster = monsterRequirement.Content;
                            if (party.MonsterKills.ContainsKey(monster.AssetName))
                            {
                                monsterRequirement.CompletedCount =
                                    party.MonsterKills[monster.AssetName];
                            }
                        }
                        // update gear requirements
                        foreach (QuestRequirement<Gear> gearRequirement in
                            quest.GearRequirements)
                        {
                            gearRequirement.CompletedCount = 0;
                            foreach (ContentEntry<Gear> entry in party.Inventory)
                            {
                                if (entry.Content == gearRequirement.Content)
                                {
                                    gearRequirement.CompletedCount += entry.Count;
                                }
                            }
                        }
                        // check to see if the requirements have been met
                        if (quest.AreRequirementsMet)
                        {
                            // immediately remove the gear
                            foreach (QuestRequirement<Gear> gearRequirement in
                                quest.GearRequirements)
                            {
                                Gear gear = gearRequirement.Content;
                                party.RemoveFromInventory(gear,
                                    gearRequirement.Count);
                            }
                            // check to see if there is a destination
                            if (String.IsNullOrEmpty(
                                quest.DestinationMapContentName))
                            {
                                // complete the quest
                                quest.Stage = Quest.QuestStage.Completed;
                                // show the completion dialogue
                                if (!String.IsNullOrEmpty(quest.CompletionMessage))
                                {
                                    DialogueScreen dialogueScreen = new DialogueScreen();
                                    dialogueScreen.TitleText = "Quest Complete";
                                    dialogueScreen.BackText = String.Empty;
                                    dialogueScreen.DialogueText =
                                        quest.CompletionMessage;
                                    ScreenManager.AddScreen(dialogueScreen);
                                }
                            }
                            else
                            {
                                quest.Stage = Quest.QuestStage.RequirementsMet;
                                // remind the player about the destination
                                screenManager.AddScreen(new QuestLogScreen(quest));
                            }
                        }
                        break;

                    case Quest.QuestStage.RequirementsMet:
                        break;

                    case Quest.QuestStage.Completed:
                        // show the quest rewards screen
                        RewardsScreen rewardsScreen =
                            new RewardsScreen(RewardsScreen.RewardScreenMode.Quest,
                            Quest.ExperienceReward, Quest.GoldReward, Quest.GearRewards);
                        screenManager.AddScreen(rewardsScreen);
                        // advance to the next quest
                        currentQuestIndex++;
                        quest = null;
                        break;
                }
            }
        }


        #endregion


        #region Modified/Removed Content


        /// <summary>
        /// The chests removed from the map asset by player actions.
        /// </summary>
        private List<WorldEntry<Chest>> removedMapChests = 
            new List<WorldEntry<Chest>>();

        /// <summary>
        /// The chests removed from the current quest asset by player actions.
        /// </summary>
        private List<WorldEntry<Chest>> removedQuestChests = 
            new List<WorldEntry<Chest>>();

        /// <summary>
        /// Remove the given chest entry from the current map or quest.
        /// </summary>
        public static void RemoveChest(MapEntry<Chest> mapEntry)
        {
            // check the parameter
            if (mapEntry == null)
            {
                return;
            }

            // check the map for the item first
            if (TileEngine.Map != null)
            {
                int removedEntries = TileEngine.Map.ChestEntries.RemoveAll(
                    delegate(MapEntry<Chest> entry)
                    {
                        return ((entry.ContentName == mapEntry.ContentName) &&
                            (entry.MapPosition == mapEntry.MapPosition));
                    });
                if (removedEntries > 0)
                {
                    WorldEntry<Chest> worldEntry = new WorldEntry<Chest>();
                    worldEntry.Content = mapEntry.Content;
                    worldEntry.ContentName = mapEntry.ContentName;
                    worldEntry.Count = mapEntry.Count;
                    worldEntry.Direction = mapEntry.Direction;
                    worldEntry.MapContentName = TileEngine.Map.AssetName;
                    worldEntry.MapPosition = mapEntry.MapPosition;
                    singleton.removedMapChests.Add(worldEntry);
                    return;
                }
            }

            // look for the map entry within the quest
            if (singleton.quest != null)
            {
                int removedEntries = singleton.quest.ChestEntries.RemoveAll(
                    delegate(WorldEntry<Chest> entry)
                    {
                        return ((entry.ContentName == mapEntry.ContentName) &&
                            (entry.MapPosition == mapEntry.MapPosition) &&
                            TileEngine.Map.AssetName.EndsWith(entry.MapContentName));
                    });
                if (removedEntries > 0)
                {
                    WorldEntry<Chest> worldEntry = new WorldEntry<Chest>();
                    worldEntry.Content = mapEntry.Content;
                    worldEntry.ContentName = mapEntry.ContentName;
                    worldEntry.Count = mapEntry.Count;
                    worldEntry.Direction = mapEntry.Direction;
                    worldEntry.MapContentName = TileEngine.Map.AssetName;
                    worldEntry.MapPosition = mapEntry.MapPosition;
                    singleton.removedQuestChests.Add(worldEntry);
                    return;
                }
            }
        }


        /// <summary>
        /// The fixed-combats removed from the map asset by player actions.
        /// </summary>
        private List<WorldEntry<FixedCombat>> removedMapFixedCombats =
            new List<WorldEntry<FixedCombat>>();

        /// <summary>
        /// The fixed-combats removed from the current quest asset by player actions.
        /// </summary>
        private List<WorldEntry<FixedCombat>> removedQuestFixedCombats =
            new List<WorldEntry<FixedCombat>>();

        /// <summary>
        /// Remove the given fixed-combat entry from the current map or quest.
        /// </summary>
        public static void RemoveFixedCombat(MapEntry<FixedCombat> mapEntry)
        {
            // check the parameter
            if (mapEntry == null)
            {
                return;
            }

            // check the map for the item first
            if (TileEngine.Map != null)
            {
                int removedEntries = TileEngine.Map.FixedCombatEntries.RemoveAll(
                    delegate(MapEntry<FixedCombat> entry)
                    {
                        return ((entry.ContentName == mapEntry.ContentName) &&
                            (entry.MapPosition == mapEntry.MapPosition));
                    });
                if (removedEntries > 0)
                {
                    WorldEntry<FixedCombat> worldEntry = new WorldEntry<FixedCombat>();
                    worldEntry.Content = mapEntry.Content;
                    worldEntry.ContentName = mapEntry.ContentName;
                    worldEntry.Count = mapEntry.Count;
                    worldEntry.Direction = mapEntry.Direction;
                    worldEntry.MapContentName = TileEngine.Map.AssetName;
                    worldEntry.MapPosition = mapEntry.MapPosition;
                    singleton.removedMapFixedCombats.Add(worldEntry);
                    return;
                }
            }

            // look for the map entry within the quest
            if (singleton.quest != null)
            {
                int removedEntries = singleton.quest.FixedCombatEntries.RemoveAll(
                    delegate(WorldEntry<FixedCombat> entry)
                    {
                        return ((entry.ContentName == mapEntry.ContentName) &&
                            (entry.MapPosition == mapEntry.MapPosition) &&
                            TileEngine.Map.AssetName.EndsWith(entry.MapContentName));
                    });
                if (removedEntries > 0)
                {
                    WorldEntry<FixedCombat> worldEntry = new WorldEntry<FixedCombat>();
                    worldEntry.Content = mapEntry.Content;
                    worldEntry.ContentName = mapEntry.ContentName;
                    worldEntry.Count = mapEntry.Count;
                    worldEntry.Direction = mapEntry.Direction;
                    worldEntry.MapContentName = TileEngine.Map.AssetName;
                    worldEntry.MapPosition = mapEntry.MapPosition;
                    singleton.removedQuestFixedCombats.Add(worldEntry);
                    return;
                }
            }
        }


        /// <summary>
        /// The player NPCs removed from the map asset by player actions.
        /// </summary>
        private List<WorldEntry<Player>> removedMapPlayerNpcs =
            new List<WorldEntry<Player>>();

        /// <summary>
        /// Remove the given player NPC entry from the current map or quest.
        /// </summary>
        public static void RemovePlayerNpc(MapEntry<Player> mapEntry)
        {
            // check the parameter
            if (mapEntry == null)
            {
                return;
            }

            // check the map for the item
            if (TileEngine.Map != null)
            {
                int removedEntries = TileEngine.Map.PlayerNpcEntries.RemoveAll(
                    delegate(MapEntry<Player> entry)
                    {
                        return ((entry.ContentName == mapEntry.ContentName) &&
                            (entry.MapPosition == mapEntry.MapPosition));
                    });
                if (removedEntries > 0)
                {
                    WorldEntry<Player> worldEntry = new WorldEntry<Player>();
                    worldEntry.Content = mapEntry.Content;
                    worldEntry.ContentName = mapEntry.ContentName;
                    worldEntry.Count = mapEntry.Count;
                    worldEntry.Direction = mapEntry.Direction;
                    worldEntry.MapContentName = TileEngine.Map.AssetName;
                    worldEntry.MapPosition = mapEntry.MapPosition;
                    singleton.removedMapPlayerNpcs.Add(worldEntry);
                    return;
                }
            }

            // quests don't have a list of player NPCs
        }


        /// <summary>
        /// The chests that have been modified, but not emptied, by player action.
        /// </summary>
        private List<ModifiedChestEntry> modifiedMapChests = 
            new List<ModifiedChestEntry>();


        /// <summary>
        /// The chests belonging to the current quest that have been modified,
        /// but not emptied, by player action.
        /// </summary>
        private List<ModifiedChestEntry> modifiedQuestChests = 
            new List<ModifiedChestEntry>();


        /// <summary>
        /// Stores the entry for a chest on the current map or quest that has been
        /// modified but not emptied.
        /// </summary>
        public static void StoreModifiedChest(MapEntry<Chest> mapEntry)
        {
            // check the parameter
            if ((mapEntry == null) || (mapEntry.Content == null))
            {
                throw new ArgumentNullException("mapEntry");
            }

            Predicate<ModifiedChestEntry> checkModifiedChests = 
                delegate(ModifiedChestEntry entry)
                {
                    return
                        (TileEngine.Map.AssetName.EndsWith(
                            entry.WorldEntry.MapContentName) &&
                        (entry.WorldEntry.ContentName == mapEntry.ContentName) &&
                        (entry.WorldEntry.MapPosition == mapEntry.MapPosition));
                };

            // check the map for the item first
            if ((TileEngine.Map != null) && TileEngine.Map.ChestEntries.Exists(
                delegate(MapEntry<Chest> entry)
                {
                    return ((entry.ContentName == mapEntry.ContentName) &&
                        (entry.MapPosition == mapEntry.MapPosition));
                }))
            {
                singleton.modifiedMapChests.RemoveAll(checkModifiedChests);
                ModifiedChestEntry modifiedChestEntry = new ModifiedChestEntry();
                modifiedChestEntry.WorldEntry.Content = mapEntry.Content;
                modifiedChestEntry.WorldEntry.ContentName = mapEntry.ContentName;
                modifiedChestEntry.WorldEntry.Count = mapEntry.Count;
                modifiedChestEntry.WorldEntry.Direction = mapEntry.Direction;
                modifiedChestEntry.WorldEntry.MapContentName = 
                    TileEngine.Map.AssetName;
                modifiedChestEntry.WorldEntry.MapPosition = mapEntry.MapPosition;
                Chest chest = mapEntry.Content;
                modifiedChestEntry.ChestEntries.AddRange(chest.Entries);
                modifiedChestEntry.Gold = chest.Gold;
                singleton.modifiedMapChests.Add(modifiedChestEntry);
                return;
            }
            

            // look for the map entry within the quest
            if ((singleton.quest != null) && singleton.quest.ChestEntries.Exists(
                delegate(WorldEntry<Chest> entry)
                {
                    return ((entry.ContentName == mapEntry.ContentName) &&
                        (entry.MapPosition == mapEntry.MapPosition) &&
                        TileEngine.Map.AssetName.EndsWith(entry.MapContentName));
                }))
            {
                singleton.modifiedQuestChests.RemoveAll(checkModifiedChests);
                ModifiedChestEntry modifiedChestEntry = new ModifiedChestEntry();
                modifiedChestEntry.WorldEntry.Content = mapEntry.Content;
                modifiedChestEntry.WorldEntry.ContentName = mapEntry.ContentName;
                modifiedChestEntry.WorldEntry.Count = mapEntry.Count;
                modifiedChestEntry.WorldEntry.Direction = mapEntry.Direction;
                modifiedChestEntry.WorldEntry.MapContentName = TileEngine.Map.AssetName;
                modifiedChestEntry.WorldEntry.MapPosition = mapEntry.MapPosition;
                Chest chest = mapEntry.Content;
                modifiedChestEntry.ChestEntries.AddRange(chest.Entries);
                modifiedChestEntry.Gold = chest.Gold;
                singleton.modifiedQuestChests.Add(modifiedChestEntry);
                return;
            }
        }


        /// <summary>
        /// Remove the specified content from the map, typically from an earlier session.
        /// </summary>
        private void ModifyMap(Map map)
        {
            // check the parameter
            if (map == null)
            {
                throw new ArgumentNullException("map");
            }

            // remove all chests that were emptied already
            if ((removedMapChests != null) && (removedMapChests.Count > 0))
            {
                // check each removed-content entry
                map.ChestEntries.RemoveAll(delegate(MapEntry<Chest> mapEntry)
                {
                    return (removedMapChests.Exists(
                        delegate(WorldEntry<Chest> removedEntry)
                        {
                            return 
                                (map.AssetName.EndsWith(removedEntry.MapContentName) &&
                                (removedEntry.ContentName == mapEntry.ContentName) &&
                                (removedEntry.MapPosition == mapEntry.MapPosition));
                        }));
                });
            }

            // remove all fixed-combats that were defeated already
            if ((removedMapFixedCombats != null) && (removedMapFixedCombats.Count > 0))
            {
                // check each removed-content entry
                map.FixedCombatEntries.RemoveAll(delegate(MapEntry<FixedCombat> mapEntry)
                {
                    return (removedMapFixedCombats.Exists(
                        delegate(WorldEntry<FixedCombat> removedEntry)
                        {
                            return
                                (map.AssetName.EndsWith(removedEntry.MapContentName) &&
                                (removedEntry.ContentName == mapEntry.ContentName) &&
                                (removedEntry.MapPosition == mapEntry.MapPosition));
                        }));
                });
            }

            // remove the player NPCs that have already joined the party
            if ((removedMapPlayerNpcs != null) && (removedMapPlayerNpcs.Count > 0))
            {
                // check each removed-content entry
                map.PlayerNpcEntries.RemoveAll(delegate(MapEntry<Player> mapEntry)
                {
                    return (removedMapPlayerNpcs.Exists(
                        delegate(WorldEntry<Player> removedEntry)
                        {
                            return
                                (map.AssetName.EndsWith(removedEntry.MapContentName) &&
                                (removedEntry.ContentName == mapEntry.ContentName) &&
                                (removedEntry.MapPosition == mapEntry.MapPosition));
                        }));
                });
            }

            // replace the chest entries of modified chests - they are already clones
            if ((modifiedMapChests != null) && (modifiedMapChests.Count > 0))
            {
                foreach (MapEntry<Chest> entry in map.ChestEntries)
                {
                    ModifiedChestEntry modifiedEntry = modifiedMapChests.Find(
                        delegate(ModifiedChestEntry modifiedTestEntry)
                        {
                            return
                                (map.AssetName.EndsWith(
                                    modifiedTestEntry.WorldEntry.MapContentName) &&
                                (modifiedTestEntry.WorldEntry.ContentName == 
                                    entry.ContentName) &&
                                (modifiedTestEntry.WorldEntry.MapPosition == 
                                    entry.MapPosition));
                        });
                    // if the chest has been modified, apply the changes
                    if (modifiedEntry != null)
                    {
                        ModifyChest(entry.Content, modifiedEntry);
                    }
                }
            }
        }


        /// <summary>
        /// Remove the specified content from the map, typically from an earlier session.
        /// </summary>
        private void ModifyQuest(Quest quest)
        {
            // check the parameter
            if (quest == null)
            {
                throw new ArgumentNullException("quest");
            }

            // remove all chests that were emptied arleady
            if ((removedQuestChests != null) && (removedQuestChests.Count > 0))
            {
                // check each removed-content entry
                quest.ChestEntries.RemoveAll(
                    delegate(WorldEntry<Chest> worldEntry)
                    {
                        return (removedQuestChests.Exists(
                            delegate(WorldEntry<Chest> removedEntry)
                            {
                                return
                                    (removedEntry.MapContentName.EndsWith(
                                        worldEntry.MapContentName) &&
                                    (removedEntry.ContentName == 
                                        worldEntry.ContentName) &&
                                    (removedEntry.MapPosition == 
                                        worldEntry.MapPosition));
                            }));
                    });
            }

            // remove all of the fixed-combats that have already been defeated
            if ((removedQuestFixedCombats != null) &&
                (removedQuestFixedCombats.Count > 0))
            {
                // check each removed-content entry
                quest.FixedCombatEntries.RemoveAll(
                    delegate(WorldEntry<FixedCombat> worldEntry)
                    {
                        return (removedQuestFixedCombats.Exists(
                            delegate(WorldEntry<FixedCombat> removedEntry)
                            {
                                return
                                    (removedEntry.MapContentName.EndsWith(
                                        worldEntry.MapContentName) &&
                                    (removedEntry.ContentName == 
                                        worldEntry.ContentName) &&
                                    (removedEntry.MapPosition == 
                                        worldEntry.MapPosition));
                            }));
                    });
            }

            // replace the chest entries of modified chests - they are already clones
            if ((modifiedQuestChests != null) && (modifiedQuestChests.Count > 0))
            {
                foreach (WorldEntry<Chest> entry in quest.ChestEntries)
                {
                    ModifiedChestEntry modifiedEntry = modifiedQuestChests.Find(
                        delegate(ModifiedChestEntry modifiedTestEntry)
                        {
                            return
                                ((modifiedTestEntry.WorldEntry.MapContentName == 
                                    entry.MapContentName) &&
                                (modifiedTestEntry.WorldEntry.ContentName == 
                                    entry.ContentName) &&
                                (modifiedTestEntry.WorldEntry.MapPosition == 
                                    entry.MapPosition));
                        });
                    // if the chest has been modified, apply the changes
                    if (modifiedEntry != null)
                    {
                        ModifyChest(entry.Content, modifiedEntry);
                    }
                }
            }
        }


        /// <summary>
        /// Modify a Chest object based on the data in a ModifiedChestEntry object.
        /// </summary>
        private void ModifyChest(Chest chest, ModifiedChestEntry modifiedChestEntry)
        {
            // check the parameters
            if ((chest == null) || (modifiedChestEntry == null))
            {
                return;
            }

            // set the new gold amount
            chest.Gold = modifiedChestEntry.Gold;

            // remove all contents not found in the modified version
            chest.Entries.RemoveAll(delegate(ContentEntry<Gear> contentEntry)
            {
                return !modifiedChestEntry.ChestEntries.Exists(
                    delegate(ContentEntry<Gear> modifiedTestEntry)
                    {
                        return (contentEntry.ContentName ==
                            modifiedTestEntry.ContentName);
                    });
            });

            // set the new counts on the remaining content items
            foreach (ContentEntry<Gear> contentEntry in chest.Entries)
            {
                ContentEntry<Gear> modifiedGearEntry =
                    modifiedChestEntry.ChestEntries.Find(
                        delegate(ContentEntry<Gear> modifiedTestEntry)
                        {
                            return (contentEntry.ContentName ==
                                modifiedTestEntry.ContentName);
                        });
                if (modifiedGearEntry != null)
                {
                    contentEntry.Count = modifiedGearEntry.Count;
                }
            }
        }


        #endregion


        #region User Interface Data


        /// <summary>
        /// The ScreenManager used to manage all UI in the game.
        /// </summary>
        private ScreenManager screenManager;

        /// <summary>
        /// The ScreenManager used to manage all UI in the game.
        /// </summary>
        public static ScreenManager ScreenManager
        {
            get { return (singleton == null ? null : singleton.screenManager); }
        }


        /// <summary>
        /// The GameplayScreen object that created this session.
        /// </summary>
        private GameplayScreen gameplayScreen;


        /// <summary>
        /// The heads-up-display menu shown on the map and combat screens.
        /// </summary>
        private Hud hud;

        /// <summary>
        /// The heads-up-display menu shown on the map and combat screens.
        /// </summary>
        public static Hud Hud
        {
            get { return (singleton == null ? null : singleton.hud); }
        }

        
        #endregion


        #region State Data


        /// <summary>
        /// Returns true if there is an active session.
        /// </summary>
        public static bool IsActive
        {
            get { return singleton != null; }
        }


        #endregion


        #region Initialization


        /// <summary>
        /// Private constructor of a Session object.
        /// </summary>
        /// <remarks>
        /// The lack of public constructors forces the singleton model.
        /// </remarks>
        private Session(ScreenManager screenManager, GameplayScreen gameplayScreen)
        {
            // check the parameter
            if (screenManager == null)
            {
                throw new ArgumentNullException("screenManager");
            }
            if (gameplayScreen == null)
            {
                throw new ArgumentNullException("gameplayScreen");
            }

            // assign the parameter
            this.screenManager = screenManager;
            this.gameplayScreen = gameplayScreen;

            // create the HUD interface
            this.hud = new Hud(screenManager);
            this.hud.LoadContent();

            loadCutscenes();
            loadTileOverrideTriggers();
        }

        public void loadCutscenes()
        {

            string CutscenePath = "Content/Maps/Cutscenes";

            DirectoryInfo di = new DirectoryInfo(CutscenePath);
            FileInfo[] rgFiles = di.GetFiles("*.*");

            int width;
            List<string> lines;
            string line;

            List<int> frames = new List<int>();
            List<string> actorNames = new List<string>();
            List<string> animationNames = new List<string>();
            List<float> xs = new List<float>();
            List<float> ys = new List<float>();

            foreach (FileInfo fi in rgFiles)
            {
                Cutscene cutscene = new Cutscene(fi.Name.Replace(".txt", ""));

                bool cutoff = false;
                lines = new List<string>();
                string[] fields;
                
                using (StreamReader reader = new StreamReader(CutscenePath + "/" + fi.Name))
                {
                    cutoff = false;

                    frames.Clear();
                    actorNames.Clear();
                    animationNames.Clear();
                    xs.Clear();
                    ys.Clear();

                    line = reader.ReadLine();
                    width = line.Length;

                    while (line != null)
                    {
                        if (line.Substring(1, 3) == "---")
                        {
                            cutoff = true;
                            line = reader.ReadLine();
                            width = line.Length;
                        }

                        if (cutoff)
                        {
                            fields = line.Split(';');

                            int frame = int.Parse(fields[0]);
                            string actorName = fields[1];
                            string animationName = fields[2];

                            bool addFrame = false;

                            for (int i = 0; i < frames.Count; i++)
                            {
                                if (frames[i] == frame && actorNames[i].Trim() == actorName.Trim())
                                    animationNames[i] = animationName;
                                else
                                {
                                    addFrame = true;
                                }
                            }

                            if(addFrame)
                            {
                                frames.Add(frame);
                                actorNames.Add(actorName);
                                animationNames.Add(animationName);
                                xs.Add(0);
                                ys.Add(0);
                            }

                        }
                        else
                        {
                            fields = line.Split(';');

                            int frame = int.Parse(fields[0]);
                            string actorName = fields[1];
                            float x = float.Parse(fields[2]);
                            float y = float.Parse(fields[3]);

                            frames.Add(frame);
                            actorNames.Add(actorName);
                            animationNames.Add("");
                            xs.Add(x);
                            ys.Add(y);
                        }

                        line = reader.ReadLine();
                    }

                    for (int i = 0; i < frames.Count; i++)
                    {
                        CutsceneFrame cutsceneFrame = new CutsceneFrame(frames[i], actorNames[i], animationNames[i], xs[i], ys[i]);
                        cutscene.frames.Add(cutsceneFrame);
                    }

                }

                cutscene.setMaxFrame();
                cutscenes.Add(cutscene);
            }

            loadCutsceneTriggers(cutscenes);
        }


        public static void loadTileOverrideTriggers()
        {
            string TileOverrideTriggerPath = "Content/Maps/TileOverrideTriggers";

            DirectoryInfo di = new DirectoryInfo(TileOverrideTriggerPath);
            FileInfo[] rgFiles = di.GetFiles("*.*");

            int width;
            List<string> lines;
            string line;

            foreach (FileInfo fi in rgFiles)
            {
                TileOverrideTrigger tileOverrideTrigger = new TileOverrideTrigger();

                tileOverrideTrigger.mapName = fi.Name.Replace(".txt", "");
                tileOverrideTrigger.overrides = new List<TileOverride>();
                tileOverrideTrigger.switchCheck = new List<MapEntry<RolePlayingGameData.Switch>>();

                bool overrideMode = false;

                lines = new List<string>();
                string[] fields;
                using (StreamReader reader = new StreamReader(TileOverrideTriggerPath + "/" + fi.Name))
                {
                    line = reader.ReadLine();
                    width = line.Length;
                    while (line != null)
                    {
                        if (line.StartsWith("NAME:"))
                        {
                            tileOverrideTrigger.name = line.Replace("NAME:","");
                            line = reader.ReadLine();
                        }

                        if (line == "OVERRIDE")
                        {
                            overrideMode = true;
                            line = reader.ReadLine();
                        }

                        if (overrideMode)
                        {
                            fields = line.Split(',');

                            int x = int.Parse(fields[0]);
                            int y = int.Parse(fields[1]);
                            int layer = int.Parse(fields[2]);
                            int newValue = int.Parse(fields[3]);

                            TileOverride tileOverride;

                            tileOverride.position = new Vector2(x,y);
                            tileOverride.layer = layer;
                            tileOverride.newValue = newValue;

                            tileOverrideTrigger.overrides.Add(tileOverride);
                        }
                        else
                        {
                            /*
                            foreach (MapEntry<RolePlayingGameData.Switch> Switch in TileEngine.Map.SwitchEntries)
                            {
                                if (Switch.ContentName == line)
                                    tileOverrideTrigger.switchCheck.Add(Switch);
                            }
                             */
                        }

                        line = reader.ReadLine();
                    }
                }

                TileOverrideTriggers.Add(tileOverrideTrigger);
            }

        }


        public static void loadCutsceneTriggers(List<Cutscene> cutscenes)
        {
            string CutsceneTriggerPath = "Content/Maps/CutsceneTriggers";

            DirectoryInfo di = new DirectoryInfo(CutsceneTriggerPath);
            FileInfo[] rgFiles = di.GetFiles("*.*");

            int width;
            List<string> lines;
            string line;

            foreach (FileInfo fi in rgFiles)
            {
                CutsceneTrigger cutsceneTrigger = new CutsceneTrigger();
                cutsceneTrigger.tiles = new List<Point>();

                foreach (Cutscene cutscene in cutscenes)
                    if (cutscene.name == fi.Name.Replace(".txt", ""))
                        cutsceneTrigger.cutscene = cutscene;

                lines = new List<string>();
                string[] fields;

                using (StreamReader reader = new StreamReader(CutsceneTriggerPath + "/" + fi.Name))
                {
                    line = reader.ReadLine();
                    width = line.Length;
                    while (line != null)
                    {
                        if (line.StartsWith("MAP:"))
                        {
                            cutsceneTrigger.mapName = line.Replace("MAP:", "");
                        }

                        if (line.StartsWith("NPC:"))
                        {
                            cutsceneTrigger.npcName = line.Replace("NPC:", "");
                        }

                        if (line.StartsWith("TILES:"))
                        {
                            fields = line.Replace("TILES:", "").Split(';');

                            foreach (string s in fields)
                                if(s != "")
                                    cutsceneTrigger.tiles.Add(new Point(int.Parse(s.Split(',')[0]), int.Parse(s.Split(',')[1])));
                        }

                        if (line.StartsWith("REPEAT"))
                        {
                            cutsceneTrigger.repeat = true;
                        }

                        if (line.StartsWith("DEACTIVATED"))
                        {
                            cutsceneTrigger.activated = true;
                        }


                        line = reader.ReadLine();
                    }
                }



                CutsceneTriggers.Add(cutsceneTrigger);
            }

        }
        #endregion


        #region Updating


        /// <summary>
        /// Update the session for this frame.
        /// </summary>
        /// <remarks>This should only be called if there are no menus in use.</remarks>
        public static void Update(GameTime gameTime)
        {
            // check the singleton
            if (singleton == null)
            {
                return;
            }

            if (CombatEngine.IsActive)
            {
                CombatEngine.Update(gameTime);
            }
            else
            {

                if (singleton.fadeTransition)
                    Session.EncounterPortal(null);

                singleton.checkDialogue();
                singleton.UpdateQuest();
                TileEngine.Update(gameTime);

                singleton.UpdateCutscene();
                singleton.UpdateMapEffects(gameTime, TileEngine.scrollMovement);
            }
        }

        public void checkDialogue()
        {
            Vector2 playerPosition = new Vector2(
                TileEngine.PartyLeaderPosition.TilePosition.X * TileEngine.Map.TileSize.X + TileEngine.PartyLeaderPosition.TileOffset.X
               , TileEngine.PartyLeaderPosition.TilePosition.Y * TileEngine.Map.TileSize.Y + TileEngine.PartyLeaderPosition.TileOffset.Y
               );

            float deltaX = 0;
            float deltaY = 0;

            for (int i = 0; i < singleton.npcs.Count; i++)
            {
                deltaX = npcPositions[i].X - playerPosition.X;
                deltaY = npcPositions[i].Y - playerPosition.Y;

                if (Math.Sqrt(deltaX * deltaX + deltaY * deltaY) < 64)
                    if ((InputManager.IsActionPressed(InputManager.Action.Ok) || InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)))
                    {
                        //check for cutscene
                        foreach(CutsceneTrigger ct in CutsceneTriggers)
                            if (ct.npcName == npcs[i].Name &&
                               ct.mapName == TileEngine.Map.Name && !ct.activated)
                            {
                                currentCutscene = ct.cutscene;
                                
                                if(!ct.repeat)
                                    ct.activated = true;
                            }

                        //normal dialogue
                        if(currentCutscene == null)
                            singleton.screenManager.AddScreen(new DialogueScreen("Dialogue Screen", "DUDE"));
                       
                    }
                   
            }


        }
        public void UpdateCutscene()
        {
            
            if(TileEngine.Map.Name != "downstairs")
                currentCutscene = null;
            

            if (currentCutscene == null)
                return;

            currentCutscene.currentFrame++;

            //pan camera
            Vector2 camPos = Vector2.Zero;

            foreach (CutsceneFrame frame in currentCutscene.frames)
                if (frame.actorName == "camera" && frame.frame == currentCutscene.currentFrame)
                    camPos = new Vector2(frame.x, frame.y);

            if (oldCamPos != Vector2.Zero)
            {
                if (camPos == Vector2.Zero)
                    camPos = oldCamPos;

                Vector2 movement = camPos - oldCamPos;

                if (movement != Vector2.Zero)
                {
                    //move camera
                    TileEngine.PartyLeaderPosition.Move(movement, false);
                }
                    
            }

            oldCamPos = camPos;

                //update player
                Player player = party.Players[0];

                foreach (CutsceneFrame frame in currentCutscene.frames)
                {
                    if (frame.frame == currentCutscene.currentFrame && player.Name == frame.actorName.Trim())
                    {
                        if (playerProxyStartPosition == Vector2.Zero)
                        {
                            //playerProxyStartPosition = new Vector2(frame.x, frame.y);
                            playerProxyStartPosition = TileEngine.GetScreenPosition(TileEngine.PartyLeaderPosition.TilePosition);
                        }

                        if (playerProxyPosition == Vector2.Zero)
                            playerProxyPosition = new Vector2(frame.x, frame.y);

                        if (frame.x != 0 || frame.y != 0)
                        {
                            Vector2 currentFramePosition = new Vector2(frame.x, frame.y);
                            playerProxyMovement += currentFramePosition - playerProxyPosition;
                            playerProxyPosition = currentFramePosition;
                        }

                        if (frame.animationName != "")
                        {
                            player.MapSprite.PlayAnimationByName(getCutsceneAnimation(frame.animationName));

                            if (frame.animationName.EndsWith("U") || frame.animationName.EndsWith("Uw"))
                                TileEngine.PartyLeaderPosition.Direction = Direction.North;
                            if (frame.animationName.EndsWith("D") || frame.animationName.EndsWith("Dw"))
                                TileEngine.PartyLeaderPosition.Direction = Direction.South;
                            if (frame.animationName.EndsWith("L") || frame.animationName.EndsWith("Lw"))
                                TileEngine.PartyLeaderPosition.Direction = Direction.West;
                            if (frame.animationName.EndsWith("R") || frame.animationName.EndsWith("Rw"))
                                TileEngine.PartyLeaderPosition.Direction = Direction.East;
                        }
                    }
                }

                playerProxyMovement += cutsceneAutoStep();
            

            
            // update NPCs
            for (int i = 0; i < singleton.npcs.Count; i++ )
            {
                    foreach (CutsceneFrame frame in currentCutscene.frames)
                    {
                        if (frame.frame == currentCutscene.currentFrame && npcs[i].Name == frame.actorName.Trim())
                        {
                            Vector2 currentPosition = new Vector2(frame.x, frame.y);

                            if(npcOldPositions[i] == Vector2.Zero)
                                npcOldPositions[i] = currentPosition;

                            if (frame.x != 0 || frame.y != 0)
                            {
                                npcPositions[i] += currentPosition - npcOldPositions[i];
                                npcOldPositions[i] = currentPosition;
                            }

                            if (frame.animationName != "")
                                npcs[i].MapSprite.PlayAnimationByName(getCutsceneAnimation(frame.animationName));
                                
                        }
                    }
                

            }

                //actions/events
                foreach (CutsceneFrame frame in currentCutscene.frames)
                {
                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("d:"))
                        singleton.screenManager.AddScreen(new DialogueScreen("Dialogue Screen", frame.actorName.Replace("d:", "")));

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("f:"))
                    {
                        foreach (MapEntry<FixedCombat> fight in TileEngine.Map.FixedCombatEntries)
                        {
                            if (fight.Content.Name == frame.actorName.Replace("f:", ""))
                                EncounterFixedCombat(fight);
                        }
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("w:"))
                    {
                        if (frame.actorName.Replace("w:", "") == party.Players[0].Name)
                            playerProxyStartPosition = new Vector2(frame.x, frame.y);

                        for (int i = 0; i < npcs.Count; i++)
                            if(frame.actorName.Replace("w:", "") == npcs[i].Name)
                                npcPositions[i] =  new Vector2(frame.x, frame.y);
                    }


                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("wmap:"))
                    {
                        for (int i = 0; i < npcs.Count; i++)
                            if (frame.actorName.Replace("wmap:", "") == npcs[i].Name)
                                npcPositions[i] = TileEngine.GetScreenPosition(npcs[i].MapPosition);
                    }


                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("wxy:"))
                    {
                        for (int i = 0; i < npcs.Count; i++)
                            if (frame.actorName.Replace("wxy:", "") == npcs[i].Name)
                                npcPositions[i] = new Vector2(TileEngine.Map.QuestNpcEntries[i].MapPosition.X, TileEngine.Map.QuestNpcEntries[i].MapPosition.Y);
                    }


                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("fp:"))
                    {
                        for (int i = 0; i < npcs.Count; i++)
                            if (frame.actorName.Replace("fp:", "") == npcs[i].Name)
                            {
                                npcs[i].Direction = facePlayer(npcPositions[i]);
                                npcs[i].ResetAnimation(false);
                            }
                    }


                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("s:"))
                    {
                        loadSong(frame.actorName.Replace("s:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("se:"))
                    {
                        loadSoundEffect(frame.actorName.Replace("se:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("r:"))
                    {
                        removeOverlay(frame.actorName.Replace("r:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("moveToTile:"))
                    {
                        moveToTile(frame.actorName.Replace("moveToTile:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("goTo:"))
                    {
                        currentCutscene.currentFrame = int.Parse(frame.actorName.Replace("goTo:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("setCam:"))
                    {
                        setCamera(frame.actorName.Replace("setCam:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("tileOverride:"))
                    {
                        activateTileOverride(frame.actorName.Replace("tileOverride:", ""));
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("act:"))
                    {
                        foreach (CutsceneTrigger ct in CutsceneTriggers)
                            if (ct.cutscene.name == frame.actorName.Replace("act:", ""))
                                
                                ct.activated = false;
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("deact:"))
                    {
                        foreach (CutsceneTrigger ct in CutsceneTriggers)
                            if (ct.cutscene.name == frame.actorName.Replace("deact:", ""))
                                ct.activated = true;
                    }

                    if (frame.frame == currentCutscene.currentFrame && frame.actorName.Contains("e:"))
                    {
                        string[] list = frame.actorName.Replace("e:", "").Split('_');
                        
                        foreach (string item in list)
                        {
                            if (item == "rain")
                            {
                                loadRain();
                            }

                            if (item == "darken")
                            {
                                loadDarken();
                            }

                            if (item == "dusk")
                            {
                                loadDusk();
                            }

                            if (item == "black")
                            {
                                loadBlack();
                            }

                            if (item == "thunder")
                            {
                                loadLightening();
                            }

                            if (item == "fog")
                            {
                                loadFog();
                            }

                            if (item == "mist")
                            {
                                loadMist();
                            }

                            if (item == "blue")
                            {
                                loadBlue();
                            }

                            if(item.StartsWith("cf:"))
                            {
                                string[] param = item.Replace("cf:", "").Split(',');
                                loadColorFade(int.Parse(param[0]), float.Parse(param[1]), float.Parse(param[2]), param[3]);
                            }
                        }
                    }
                }

            //end cutscene
            if (currentCutscene.currentFrame == currentCutscene.maxFrame)
            {
                currentCutscene.currentFrame = 0;
                currentCutscene = null;

                
                //update player position
                Vector2 playerCustsceneDist = playerProxyStartPosition + playerProxyMovement - TileEngine.PartyLeaderPosition.ScreenPosition;
                //TileEngine.PartyLeaderPosition.Move(playerCustsceneDist, false);
                
                // update NPCs positions
                for (int i = 0; i < singleton.npcs.Count; i++)
                    npcOldPositions[i] = Vector2.Zero;

                playerProxyMovement = Vector2.Zero;
                playerProxyPosition = Vector2.Zero;
                playerProxyStartPosition = Vector2.Zero;
                playerProxyAutoMove = Vector2.Zero;

                oldCamPos = Vector2.Zero;
            }

        }

        public void UpdateMapEffects(GameTime gameTime, Vector2 movement)
        {
            if (raindrops.Count > 0)
            {
                foreach (Raindrop rain in raindrops)
                {
                    rain.UpdateRainAnimation((float)gameTime.ElapsedGameTime.TotalSeconds, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height, movement);
                }
            }


            if(mapOverlays.Count > 0)
            {
                singleton.garbageOverlays = new List<MapOverlay>();

                foreach (MapOverlay overlay in mapOverlays)
                {
                    overlay.Update((float)gameTime.ElapsedGameTime.TotalSeconds, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height, movement + overlay.drift);

                    //remove completed fade in/out
                    if (overlay.name == "fade" && overlay.lifeTimer == MAP_TRANSITION_FADE_TIME)
                        singleton.garbageOverlays.Add(overlay);

                    if (overlay.name == "colorfade")
                        if (overlay.deactivated || overlay.opacity < 0.01f)
                            singleton.garbageOverlays.Add(overlay);
                }

                foreach (MapOverlay overlay in singleton.garbageOverlays)
                {
                    mapOverlays.Remove(overlay);

                    if (overlay.opacity > 0.99f && overlay.name == "fade")
                        loadBlack();
                }


            }
        }

        public static Vector2 cutsceneAutoStep()
        {
            Vector2 autoStep = Vector2.Zero;

            if (singleton.playerProxyAutoMove != Vector2.Zero)
            {
                if (Math.Abs(singleton.playerProxyAutoMove.X) > TileEngine.partyLeaderMovementSpeed)
                {
                    if (singleton.playerProxyAutoMove.X > 0)
                    { 
                        autoStep.X = TileEngine.partyLeaderMovementSpeed;
                        singleton.playerProxyAutoMove.X -= TileEngine.partyLeaderMovementSpeed;
                    }
                    else
                    {
                        autoStep.X = -TileEngine.partyLeaderMovementSpeed;
                        singleton.playerProxyAutoMove.X += TileEngine.partyLeaderMovementSpeed;
                    }
                }
                else
                {
                    autoStep.X = singleton.playerProxyAutoMove.X;
                    singleton.playerProxyAutoMove.X = 0;
                }

                if (Math.Abs(singleton.playerProxyAutoMove.Y) > TileEngine.partyLeaderMovementSpeed)
                {
                    if (singleton.playerProxyAutoMove.Y > 0)
                    {
                        autoStep.Y = TileEngine.partyLeaderMovementSpeed;
                        singleton.playerProxyAutoMove.Y -= TileEngine.partyLeaderMovementSpeed;
                    }
                    else
                    {
                        autoStep.Y = -TileEngine.partyLeaderMovementSpeed;
                        singleton.playerProxyAutoMove.Y += TileEngine.partyLeaderMovementSpeed;
                    }
                }
                else
                {
                    autoStep.Y = singleton.playerProxyAutoMove.Y;
                    singleton.playerProxyAutoMove.Y = 0;
                }
            }

            //TileEngine.PartyLeaderPosition.Move(autoStep);
            return autoStep;
        }

        public static void activateTileOverride(string overrideName)
        {
            foreach (TileOverrideTrigger over in TileOverrideTriggers)
                if (TileEngine.Map.Name.StartsWith(over.mapName) && over.name == overrideName)
                    over.active = true;
        }

        

        #endregion


        #region Drawing


        /// <summary>
        /// Draws the session environment to the screen
        /// </summary>
        public static void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = singleton.screenManager.SpriteBatch;

            if (CombatEngine.IsActive)
            {
                // draw the combat background
                if (TileEngine.Map.CombatTexture != null)
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(TileEngine.Map.CombatTexture, Vector2.Zero, 
                        Color.White);
                    spriteBatch.End();
                }

                // draw the combat itself
                spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                CombatEngine.Draw(gameTime);
                spriteBatch.End();
            }
            else
            {
                singleton.DrawNonCombat(gameTime);
            }

            singleton.hud.Draw();
        }


        /// <summary>
        /// Draws everything related to the non-combat part of the screen
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        private void DrawNonCombat(GameTime gameTime)
        {
            SpriteBatch spriteBatch = screenManager.SpriteBatch;

            // draw the background
            //spriteBatch.Begin();

           // spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            spriteBatch.Begin();
            /*
            SamplerState samst = new SamplerState();
            samst.Filter = TextureFilter.Point;

            spriteBatch.GraphicsDevice.SamplerStates[0] = samst;
            */


            /*
            if (Session.holdButton)
            {
                Texture2D printTex = singleton.screenManager.Game.Content.Load<Texture2D>(@"Textures\Maps\NonCombat\indoors1");

                TileEngine.PrintMap(spriteBatch, true, false, false, printTex);
                TileEngine.PrintMap(spriteBatch, false, true, false, printTex);
                TileEngine.PrintMap(spriteBatch, false, false, true, printTex);
            }
            */

            if (TileEngine.Map.Texture != null)
            {
                // draw the ground layer
                TileEngine.DrawLayers(spriteBatch, true, true, false);
                // draw the character shadows
                //DrawShadows(spriteBatch);
            }
            spriteBatch.End();

            // Sort the object layer and player according to depth 
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // draw the party leader
            {
                Player player = party.Players[0];
                Vector2 position = TileEngine.PartyLeaderPosition.ScreenPosition;
                player.Direction = TileEngine.PartyLeaderPosition.Direction;

                // apply cutscene position
                if (currentCutscene != null)
                {
                    player.MapSprite.UpdateAnimation(elapsedSeconds);
                    player.MapSprite.Draw(spriteBatch, playerProxyStartPosition + playerProxyMovement, 1f - (playerProxyStartPosition.Y + playerProxyMovement.Y) / (float)TileEngine.Viewport.Height);   
                }
                else
                {
                    player.ResetAnimation(TileEngine.PartyLeaderPosition.IsMoving);
                    switch (player.State)
                    {
                        case Character.CharacterState.Idle:
                            if (player.MapSprite != null)
                            {
                                player.MapSprite.UpdateAnimation(elapsedSeconds);
                                player.MapSprite.Draw(spriteBatch, position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            break;

                        case Character.CharacterState.Walking:
                            if (player.WalkingSprite != null)
                            {
                                player.WalkingSprite.UpdateAnimation(elapsedSeconds);
                                player.WalkingSprite.Draw(spriteBatch, position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            else if (player.MapSprite != null)
                            {
                                player.MapSprite.UpdateAnimation(elapsedSeconds);
                                player.MapSprite.Draw(spriteBatch, position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            break;
                    }
                }
            }

            #region player NPCs
            // draw the player NPCs
            foreach (MapEntry<Player> playerEntry in TileEngine.Map.PlayerNpcEntries)
            {
                if (playerEntry.Content == null)
                {
                    continue;
                }

                Vector2 position = TileEngine.GetScreenPosition(playerEntry.MapPosition);

                playerEntry.Content.ResetAnimation(false);
                switch (playerEntry.Content.State)
                {
                    case Character.CharacterState.Idle:
                        if (playerEntry.Content.MapSprite != null)
                        {
                            playerEntry.Content.MapSprite.UpdateAnimation(
                                elapsedSeconds);
                            playerEntry.Content.MapSprite.Draw(spriteBatch, position,
                                1f - position.Y / (float)TileEngine.Viewport.Height);
                        }
                        break;

                    case Character.CharacterState.Walking:
                        if (playerEntry.Content.WalkingSprite != null)
                        {
                            playerEntry.Content.WalkingSprite.UpdateAnimation(
                                elapsedSeconds);
                            playerEntry.Content.WalkingSprite.Draw(spriteBatch, position,
                                1f - position.Y / (float)TileEngine.Viewport.Height);
                        }
                        else if (playerEntry.Content.MapSprite != null)
                        {
                            playerEntry.Content.MapSprite.UpdateAnimation(
                                elapsedSeconds);
                            playerEntry.Content.MapSprite.Draw(spriteBatch, position,
                                1f - position.Y / (float)TileEngine.Viewport.Height);
                        }
                        break;
                }
            }
            #endregion

            // draw the quest NPCs
            foreach (MapEntry<QuestNpc> questNpcEntry in TileEngine.Map.QuestNpcEntries)
            {
                if (questNpcEntry.Content == null)
                {
                    continue;
                }

                Vector2 position = Vector2.Zero;

                //position = TileEngine.GetScreenPosition(questNpcEntry.MapPosition);

                if (currentCutscene != null)
                {
                    for (int i = 0; i < npcs.Count; i++)
                        if (npcs[i].Name == questNpcEntry.Content.Name)
                        {
                            questNpcEntry.Content.MapSprite.UpdateAnimation(elapsedSeconds);
                            questNpcEntry.Content.MapSprite.Draw(spriteBatch, npcPositions[i] + TileEngine.mapOriginPosition, 1f - (TileEngine.mapOriginPosition.Y + npcPositions[i].Y) / (float)TileEngine.Viewport.Height);
                        }
                }
                else
                {
                    for (int i = 0; i < npcs.Count; i++)
                        if (npcs[i].Name == questNpcEntry.Content.Name)
                            position = npcPositions[i] + TileEngine.mapOriginPosition;

                    questNpcEntry.Content.ResetAnimation(false);
                    switch (questNpcEntry.Content.State)
                    {
                        case Character.CharacterState.Idle:
                            if (questNpcEntry.Content.MapSprite != null)
                            {
                                questNpcEntry.Content.MapSprite.UpdateAnimation(
                                    elapsedSeconds);
                                questNpcEntry.Content.MapSprite.Draw(spriteBatch, position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            break;

                        case Character.CharacterState.Walking:
                            if (questNpcEntry.Content.WalkingSprite != null)
                            {
                                questNpcEntry.Content.WalkingSprite.UpdateAnimation(
                                    elapsedSeconds);
                                questNpcEntry.Content.WalkingSprite.Draw(spriteBatch,
                                    position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            else if (questNpcEntry.Content.MapSprite != null)
                            {
                                questNpcEntry.Content.MapSprite.UpdateAnimation(
                                    elapsedSeconds);
                                questNpcEntry.Content.MapSprite.Draw(spriteBatch, position,
                                    1f - position.Y / (float)TileEngine.Viewport.Height);
                            }
                            break;
                    }
                }


            }

            // draw the fixed-combat monsters NPCs from the TileEngine.Map
            // -- since there may be many of the same FixedCombat object 
            //    on the TileEngine.Map, but their animations are handled differently
            foreach (MapEntry<FixedCombat> fixedCombatEntry in 
                TileEngine.Map.FixedCombatEntries)
            {
                if ((fixedCombatEntry.Content == null) || 
                    (fixedCombatEntry.Content.Entries.Count <= 0))
                {
                    continue;
                }
                Vector2 position = 
                    TileEngine.GetScreenPosition(fixedCombatEntry.MapPosition);
                fixedCombatEntry.MapSprite.UpdateAnimation(elapsedSeconds);
                fixedCombatEntry.MapSprite.Draw(spriteBatch, position,
                    1f - position.Y / (float)TileEngine.Viewport.Height);
            }

            // draw the fixed-combat monsters NPCs from the current quest
            // -- since there may be many of the same FixedCombat object 
            //    on the TileEngine.Map, their animations are handled differently
            if ((quest != null) && ((quest.Stage == Quest.QuestStage.InProgress) ||
                (quest.Stage == Quest.QuestStage.RequirementsMet)))
            {
                foreach (WorldEntry<FixedCombat> fixedCombatEntry 
                    in quest.FixedCombatEntries)
                {
                    if ((fixedCombatEntry.Content == null) || 
                        (fixedCombatEntry.Content.Entries.Count <= 0) ||
                        !TileEngine.Map.AssetName.EndsWith(
                            fixedCombatEntry.MapContentName))
                    {
                        continue;
                    }
                    Vector2 position =
                        TileEngine.GetScreenPosition(fixedCombatEntry.MapPosition);
                    fixedCombatEntry.MapSprite.UpdateAnimation(elapsedSeconds);
                    fixedCombatEntry.MapSprite.Draw(spriteBatch, position,
                        1f - position.Y / (float)TileEngine.Viewport.Height);
                }
            }

            // draw the chests from the TileEngine.Map
            foreach (MapEntry<Chest> chestEntry in TileEngine.Map.ChestEntries)
            {
                if (chestEntry.Content == null)
                {
                    continue;
                }
                Vector2 position = TileEngine.GetScreenPosition(chestEntry.MapPosition);
                spriteBatch.Draw(chestEntry.Content.Texture,
                    position, null, Color.White, 0f, Vector2.Zero, 1f,
                    SpriteEffects.None,
                    MathHelper.Clamp(1f - position.Y / 
                        (float)TileEngine.Viewport.Height, 0f, 1f));
            }

            // draw the chests from the current quest
            if ((quest != null) && ((quest.Stage == Quest.QuestStage.InProgress) ||
                (quest.Stage == Quest.QuestStage.RequirementsMet)))
            {
                foreach (WorldEntry<Chest> chestEntry in quest.ChestEntries)
                {
                    if ((chestEntry.Content == null) || 
                        !TileEngine.Map.AssetName.EndsWith(chestEntry.MapContentName))
                    {
                        continue;
                    }
                    Vector2 position = 
                        TileEngine.GetScreenPosition(chestEntry.MapPosition);
                    spriteBatch.Draw(chestEntry.Content.Texture,
                        position, null, Color.White, 0f, Vector2.Zero, 1f,
                        SpriteEffects.None,
                        MathHelper.Clamp(1f - position.Y / 
                            (float)TileEngine.Viewport.Height, 0f, 1f));
                }
            }


            //draw switches
            foreach (MapEntry<RolePlayingGameData.Switch> switchEntry in TileEngine.Map.SwitchEntries)
            {
                Vector2 position = new Vector2(
                    TileEngine.mapOriginPosition.X + switchEntry.Content.Position.X,
                    TileEngine.mapOriginPosition.Y + switchEntry.Content.Position.Y);

                Texture2D switchTex;

                if (switchEntry.Content.Active)
                    switchTex = switchEntry.Content.OnTexture;
                else
                    switchTex = switchEntry.Content.OffTexture;

                spriteBatch.Draw(
                    switchTex,
                    position,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    1f
                    );

            }


            //draw blocks
            foreach(MapEntry<Block> blockEntry in TileEngine.Map.BlockEntries)
            {
                Vector2 position =  new Vector2(
                    TileEngine.mapOriginPosition.X + blockEntry.Content.Position.X,
                    TileEngine.mapOriginPosition.Y + blockEntry.Content.Position.Y);

                spriteBatch.Draw(
                    blockEntry.Content.Texture,
                    position, 
                    null, 
                    Color.White, 
                    0f, 
                    Vector2.Zero, 
                    1f,
                    SpriteEffects.None,
                    MathHelper.Clamp(1f - position.Y /(float)TileEngine.Viewport.Height, 0f, 1f)
                    );

            }

            spriteBatch.End();

            // draw the foreground
            spriteBatch.Begin();
            if (TileEngine.Map.Texture != null)
            {
                TileEngine.DrawLayers(spriteBatch, false, false, true);
            }

            // draw map effects
            DrawMapEffects(spriteBatch);


            spriteBatch.End();
        }

        private Animation getCutsceneAnimation(string animationName)
        {
            Animation result = new Animation();

            if (animationName.Substring(animationName.Length - 2, 2) == "Dw")
                result = new Animation("WalkSouth", 1, 4, 160, true);
            if (animationName.Substring(animationName.Length - 2, 2) == "Uw")
                result = new Animation("WalkNorth", 5, 8, 160, true);
            if (animationName.Substring(animationName.Length - 2, 2) == "Lw")
                result = new Animation("WalkWest", 9, 12, 160, true);
            if (animationName.Substring(animationName.Length - 2, 2) == "Rw")
                result = new Animation("WalkEast", 13, 16, 160, true);

            if (animationName.Substring(animationName.Length - 1, 1) == "D")
                result = new Animation("IdleSouth", 2, 2, 200, true);
            if (animationName.Substring(animationName.Length - 1, 1) == "U")
                result = new Animation("IdleNorth", 6, 6, 200, true);
            if (animationName.Substring(animationName.Length - 1, 1) == "L")
                result = new Animation("IdleWest", 10, 10, 200, true);
            if (animationName.Substring(animationName.Length - 1, 1) == "R")
                result = new Animation("IdleEast", 14, 14, 200, true);

            if (animationName.Substring(animationName.Length - 1, 1) == "S")
                result = new Animation("Still", 1, 1, 200, true);

            return result;

        }

        private void DrawMapEffects(SpriteBatch spriteBatch)
        {
            // draw the rain

            if (raindrops.Count > 0)
            {
                foreach (Raindrop rain in raindrops)
                {
                    Rectangle srcRect = new Rectangle(rain.rowOffset, 0, rain.width, rain.width * 2);

                    spriteBatch.Draw(rain.Texture, rain.position, srcRect, Color.White);
                }
            }

            // draw the mapOverlays
            if (mapOverlays.Count > 0)
            {
                Rectangle srcRect;

                foreach (MapOverlay overlay in mapOverlays)
                {
                    foreach(OverlayPane pane in overlay.overlays)
                    {
                        srcRect = new Rectangle(pane.rowOffset, 0, pane.width, pane.height);
                        spriteBatch.Draw(pane.Texture, pane.position, srcRect, Color.White * overlay.opacity);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the shadows that appear under all characters.
        /// </summary>
        private void DrawShadows(SpriteBatch spriteBatch)
        {
            // draw the shadow of the party leader
            Player player = party.Players[0];
            if (player.ShadowTexture != null)
            {
                spriteBatch.Draw(player.ShadowTexture, 
                    TileEngine.PartyLeaderPosition.ScreenPosition, null, Color.White, 0f,
                    new Vector2(
                        (player.ShadowTexture.Width - TileEngine.Map.TileSize.X) / 2,
                        (player.ShadowTexture.Height - TileEngine.Map.TileSize.Y) / 2 - 
                            player.ShadowTexture.Height / 6), 
                    1f, SpriteEffects.None, 1f);
            }

            // draw the player NPCs' shadows
            foreach (MapEntry<Player> playerEntry in TileEngine.Map.PlayerNpcEntries)
            {
                if (playerEntry.Content == null)
                {
                    continue;
                }
                if (playerEntry.Content.ShadowTexture != null)
                {
                    Vector2 position = 
                        TileEngine.GetScreenPosition(playerEntry.MapPosition);
                    spriteBatch.Draw(playerEntry.Content.ShadowTexture, position,
                        null, Color.White, 0f, 
                        new Vector2(
                        (playerEntry.Content.ShadowTexture.Width - 
                            TileEngine.Map.TileSize.X) / 2,
                        (playerEntry.Content.ShadowTexture.Height - 
                            TileEngine.Map.TileSize.Y) / 2 - 
                            playerEntry.Content.ShadowTexture.Height / 6),
                        1f, SpriteEffects.None, 1f);
                }
            }

            // draw the quest NPCs' shadows
            foreach (MapEntry<QuestNpc> questNpcEntry in TileEngine.Map.QuestNpcEntries)
            {
                if (questNpcEntry.Content == null)
                {
                    continue;
                }
                if (questNpcEntry.Content.ShadowTexture != null)
                {
                    Vector2 position = 
                        TileEngine.GetScreenPosition(questNpcEntry.MapPosition);
                    spriteBatch.Draw(questNpcEntry.Content.ShadowTexture, position,
                        null, Color.White, 0f,
                        new Vector2(
                            (questNpcEntry.Content.ShadowTexture.Width - 
                                TileEngine.Map.TileSize.X) / 2,
                            (questNpcEntry.Content.ShadowTexture.Height - 
                                TileEngine.Map.TileSize.Y) / 2 - 
                                questNpcEntry.Content.ShadowTexture.Height / 6),
                        1f, SpriteEffects.None, 1f);
                }
            }

            // draw the fixed-combat monsters NPCs' shadows
            foreach (MapEntry<FixedCombat> fixedCombatEntry in 
                TileEngine.Map.FixedCombatEntries)
            {
                if ((fixedCombatEntry.Content == null) || 
                    (fixedCombatEntry.Content.Entries.Count <= 0))
                {
                    continue;
                }
                Monster monster = fixedCombatEntry.Content.Entries[0].Content;
                if (monster.ShadowTexture != null)
                {
                    Vector2 position = 
                        TileEngine.GetScreenPosition(fixedCombatEntry.MapPosition);
                    spriteBatch.Draw(monster.ShadowTexture, position,
                        null, Color.White, 0f,
                        new Vector2(
                        (monster.ShadowTexture.Width - TileEngine.Map.TileSize.X) / 2,
                        (monster.ShadowTexture.Height - TileEngine.Map.TileSize.Y) / 2 - 
                            monster.ShadowTexture.Height / 6),
                        1f, SpriteEffects.None, 1f);
                }
            }



        }


        #endregion


        #region Starting a New Session


        /// <summary>
        /// Start a new session based on the data provided.
        /// </summary>
        public static void StartNewSession(GameStartDescription gameStartDescription, 
            ScreenManager screenManager, GameplayScreen gameplayScreen)
        {
            // check the parameters
            if (gameStartDescription == null)
            {
                throw new ArgumentNullException("gameStartDescripton");
            }
            if (screenManager == null)
            {
                throw new ArgumentNullException("screenManager");
            }
            if (gameplayScreen == null)
            {
                throw new ArgumentNullException("gameplayScreen");
            }

            // end any existing session
            EndSession();

            // create a new singleton
            singleton = new Session(screenManager, gameplayScreen);

            // set up the initial map
            ChangeMap(gameStartDescription.MapContentName, null);

            // set up the initial party
            ContentManager content = singleton.screenManager.Game.Content;
            singleton.party = new Party(gameStartDescription, content);

            // load the quest line
            singleton.questLine = content.Load<QuestLine>(
                Path.Combine(@"Quests\QuestLines", 
                gameStartDescription.QuestLineContentName)).Clone() as QuestLine;

            singleton.questLine = null;
        }


        #endregion


        #region Ending a Session


        /// <summary>
        /// End the current session.
        /// </summary>
        public static void EndSession()
        {
            // exit the gameplay screen
            // -- store the gameplay session, for re-entrance
            if (singleton != null)
            {
                GameplayScreen gameplayScreen = singleton.gameplayScreen;
                singleton.gameplayScreen = null;

                // pop the music
                AudioManager.PopMusic();

                // clear the singleton
                singleton = null;

                if (gameplayScreen != null)
                {
                    gameplayScreen.ExitScreen();
                }
            }
        }


        #endregion


        #region Loading a Session


        /// <summary>
        /// Start a new session, using the data in the given save game.
        /// </summary>
        /// <param name="saveGameDescription">The description of the save game.</param>
        /// <param name="screenManager">The ScreenManager for the new session.</param>
        public static void LoadSession(SaveGameDescription saveGameDescription,
            ScreenManager screenManager, GameplayScreen gameplayScreen)
        {
            // check the parameters
            if (saveGameDescription == null)
            {
                throw new ArgumentNullException("saveGameDescription");
            }
            if (screenManager == null)
            {
                throw new ArgumentNullException("screenManager");
            }
            if (gameplayScreen == null)
            {
                throw new ArgumentNullException("gameplayScreen");
            }

            // end any existing session
            EndSession();

            // create the new session
            singleton = new Session(screenManager, gameplayScreen);

            // get the storage device and load the session
            GetStorageDevice(
                delegate(StorageDevice storageDevice)
                {
                    LoadSessionResult(storageDevice, saveGameDescription);
                });
        }


        /// <summary>
        /// Receives the storage device and starts a new session, 
        /// using the data in the given save game.
        /// </summary>
        /// <remarks>The new session is created in LoadSessionResult.</remarks>
        /// <param name="storageDevice">The chosen storage device.</param>
        /// <param name="saveGameDescription">The description of the save game.</param>
        public static void LoadSessionResult(StorageDevice storageDevice, 
            SaveGameDescription saveGameDescription)
        {
            // check the parameters
            if (saveGameDescription == null)
            {
                throw new ArgumentNullException("saveGameDescription");
            }
            // check the parameter
            if ((storageDevice == null) || !storageDevice.IsConnected)
            {
                return;
            }

            // open the container
            using (StorageContainer storageContainer = OpenContainer(storageDevice))
            {
                using (Stream stream = 
                    storageContainer.OpenFile(saveGameDescription.FileName, FileMode.Open))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stream))
                    {
                        // <rolePlayingGameData>
                        xmlReader.ReadStartElement("rolePlayingGameSaveData");

                        // read the map information
                        xmlReader.ReadStartElement("mapData");
                        string mapAssetName =
                            xmlReader.ReadElementString("mapContentName");
                        PlayerPosition playerPosition = new XmlSerializer(
                            typeof(PlayerPosition)).Deserialize(xmlReader)
                            as PlayerPosition;
                        singleton.removedMapChests = new XmlSerializer(
                            typeof(List<WorldEntry<Chest>>)).Deserialize(xmlReader)
                            as List<WorldEntry<Chest>>;
                        singleton.removedMapFixedCombats = new XmlSerializer(
                            typeof(List<WorldEntry<FixedCombat>>)).Deserialize(xmlReader)
                            as List<WorldEntry<FixedCombat>>;
                        singleton.removedMapPlayerNpcs = new XmlSerializer(
                            typeof(List<WorldEntry<Player>>)).Deserialize(xmlReader)
                            as List<WorldEntry<Player>>;
                        singleton.modifiedMapChests = new XmlSerializer(
                            typeof(List<ModifiedChestEntry>)).Deserialize(xmlReader)
                            as List<ModifiedChestEntry>;
                        ChangeMap(mapAssetName, null);
                        TileEngine.PartyLeaderPosition = playerPosition;
                        xmlReader.ReadEndElement();

                        // read the quest information
                        ContentManager content = Session.ScreenManager.Game.Content;
                        xmlReader.ReadStartElement("questData");
                        singleton.questLine = content.Load<QuestLine>(
                            xmlReader.ReadElementString("questLineContentName")).Clone()
                            as QuestLine;
                        singleton.currentQuestIndex = Convert.ToInt32(
                            xmlReader.ReadElementString("currentQuestIndex"));
                        for (int i = 0; i < singleton.currentQuestIndex; i++)
                        {
                            singleton.questLine.Quests[i].Stage =
                                Quest.QuestStage.Completed;
                        }
                        singleton.removedQuestChests = new XmlSerializer(
                            typeof(List<WorldEntry<Chest>>)).Deserialize(xmlReader)
                            as List<WorldEntry<Chest>>;
                        singleton.removedQuestFixedCombats = new XmlSerializer(
                            typeof(List<WorldEntry<FixedCombat>>)).Deserialize(xmlReader)
                            as List<WorldEntry<FixedCombat>>;
                        singleton.modifiedQuestChests = new XmlSerializer(
                            typeof(List<ModifiedChestEntry>)).Deserialize(xmlReader)
                            as List<ModifiedChestEntry>;
                        Quest.QuestStage questStage = (Quest.QuestStage)Enum.Parse(
                            typeof(Quest.QuestStage),
                            xmlReader.ReadElementString("currentQuestStage"), true);
                        if ((singleton.questLine != null) && !IsQuestLineComplete)
                        {
                            singleton.quest =
                                singleton.questLine.Quests[CurrentQuestIndex];
                            singleton.ModifyQuest(singleton.quest);
                            singleton.quest.Stage = questStage;
                        }
                        xmlReader.ReadEndElement();

                        // read the party data
                        singleton.party = new Party(new XmlSerializer(
                            typeof(PartySaveData)).Deserialize(xmlReader)
                            as PartySaveData, content);

                        // </rolePlayingGameSaveData>
                        xmlReader.ReadEndElement();
                    }
                }
            }
        }


        #endregion


        #region Saving the Session


        /// <summary>
        /// Save the current state of the session.
        /// </summary>
        /// <param name="overwriteDescription">
        /// The description of the save game to over-write, if any.
        /// </param>
        public static void SaveSession(SaveGameDescription overwriteDescription)
        {
            // retrieve the storage device, asynchronously
            GetStorageDevice(delegate(StorageDevice storageDevice)
            {
                SaveSessionResult(storageDevice, overwriteDescription);
            });
        }


        /// <summary>
        /// Save the current state of the session, with the given storage device.
        /// </summary>
        /// <param name="storageDevice">The chosen storage device.</param>
        /// <param name="overwriteDescription">
        /// The description of the save game to over-write, if any.
        /// </param>
        private static void SaveSessionResult(StorageDevice storageDevice, 
            SaveGameDescription overwriteDescription)
        {
            // check the parameter
            if ((storageDevice == null) || !storageDevice.IsConnected)
            {
                return;
            }

            // open the container
            using (StorageContainer storageContainer =
                OpenContainer(storageDevice))
            {
                string filename;
                string descriptionFilename;
                // get the filenames
                if (overwriteDescription == null)
                {
                    int saveGameIndex = 0;
                    string testFilename;
                    do
                    {
                        saveGameIndex++;
                        testFilename = "SaveGame" + saveGameIndex.ToString() + ".xml";
                    }
                    while (storageContainer.FileExists(testFilename));
                    filename = testFilename;
                    descriptionFilename = "SaveGameDescription" +
                        saveGameIndex.ToString() + ".xml";
                }
                else
                {
                    filename = overwriteDescription.FileName;
                    descriptionFilename = "SaveGameDescription" +
                        Path.GetFileNameWithoutExtension(
                        overwriteDescription.FileName).Substring(8) + ".xml";
                }
                using (Stream stream = storageContainer.OpenFile(filename, FileMode.Create))
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stream))
                    {
                        // <rolePlayingGameData>
                        xmlWriter.WriteStartElement("rolePlayingGameSaveData");

                        // write the map information
                        xmlWriter.WriteStartElement("mapData");
                        xmlWriter.WriteElementString("mapContentName",
                            TileEngine.Map.AssetName);
                        new XmlSerializer(typeof(PlayerPosition)).Serialize(
                            xmlWriter, TileEngine.PartyLeaderPosition);
                        new XmlSerializer(typeof(List<WorldEntry<Chest>>)).Serialize(
                            xmlWriter, singleton.removedMapChests);
                        new XmlSerializer(
                            typeof(List<WorldEntry<FixedCombat>>)).Serialize(
                            xmlWriter, singleton.removedMapFixedCombats);
                        new XmlSerializer(typeof(List<WorldEntry<Player>>)).Serialize(
                            xmlWriter, singleton.removedMapPlayerNpcs);
                        new XmlSerializer(typeof(List<ModifiedChestEntry>)).Serialize(
                            xmlWriter, singleton.modifiedMapChests);
                        xmlWriter.WriteEndElement();

                        // write the quest information
                        xmlWriter.WriteStartElement("questData");
                        xmlWriter.WriteElementString("questLineContentName",
                            singleton.questLine.AssetName);
                        xmlWriter.WriteElementString("currentQuestIndex",
                            singleton.currentQuestIndex.ToString());
                        new XmlSerializer(typeof(List<WorldEntry<Chest>>)).Serialize(
                            xmlWriter, singleton.removedQuestChests);
                        new XmlSerializer(
                            typeof(List<WorldEntry<FixedCombat>>)).Serialize(
                            xmlWriter, singleton.removedQuestFixedCombats);
                        new XmlSerializer(typeof(List<ModifiedChestEntry>)).Serialize(
                            xmlWriter, singleton.modifiedQuestChests);
                        xmlWriter.WriteElementString("currentQuestStage",
                            IsQuestLineComplete ?
                            Quest.QuestStage.NotStarted.ToString() :
                            singleton.quest.Stage.ToString());
                        xmlWriter.WriteEndElement();

                        // write the party data
                        new XmlSerializer(typeof(PartySaveData)).Serialize(xmlWriter,
                            new PartySaveData(singleton.party));

                        // </rolePlayingGameSaveData>
                        xmlWriter.WriteEndElement();
                    }
                }

                // create the save game description
                SaveGameDescription description = new SaveGameDescription();
                description.FileName = Path.GetFileName(filename);
                description.ChapterName = IsQuestLineComplete ? "Quest Line Complete" :
                    Quest.Name;
                description.Description = DateTime.Now.ToString();
                using (Stream stream = 
                    storageContainer.OpenFile(descriptionFilename, FileMode.Create))
                {
                    new XmlSerializer(typeof(SaveGameDescription)).Serialize(stream,
                        description);
                }
            }
        }


        #endregion


        #region Deleting a Save Game


        /// <summary>
        /// Delete the save game specified by the description.
        /// </summary>
        /// <param name="saveGameDescription">The description of the save game.</param>
        public static void DeleteSaveGame(SaveGameDescription saveGameDescription)
        {
            // check the parameters
            if (saveGameDescription == null)
            {
                throw new ArgumentNullException("saveGameDescription");
            }

            // get the storage device and load the session
            GetStorageDevice(
                delegate(StorageDevice storageDevice)
                {
                    DeleteSaveGameResult(storageDevice, saveGameDescription);
                });
        }


        /// <summary>
        /// Delete the save game specified by the description.
        /// </summary>
        /// <param name="storageDevice">The chosen storage device.</param>
        /// <param name="saveGameDescription">The description of the save game.</param>
        public static void DeleteSaveGameResult(StorageDevice storageDevice,
            SaveGameDescription saveGameDescription)
        {
            // check the parameters
            if (saveGameDescription == null)
            {
                throw new ArgumentNullException("saveGameDescription");
            }
            // check the parameter
            if ((storageDevice == null) || !storageDevice.IsConnected)
            {
                return;
            }

            // open the container
            using (StorageContainer storageContainer =
                OpenContainer(storageDevice))
            {
                storageContainer.DeleteFile(saveGameDescription.FileName);
                storageContainer.DeleteFile("SaveGameDescription" +
                    Path.GetFileNameWithoutExtension(
                        saveGameDescription.FileName).Substring(8) + ".xml");
            }

            // refresh the save game descriptions
            Session.RefreshSaveGameDescriptions();
        }


        #endregion


        #region Save Game Descriptions


        /// <summary>
        /// Save game descriptions for the current set of save games.
        /// </summary>
        private static List<SaveGameDescription> saveGameDescriptions = null;

        /// <summary>
        /// Save game descriptions for the current set of save games.
        /// </summary>
        public static List<SaveGameDescription> SaveGameDescriptions
        {
            get { return saveGameDescriptions; }
        }


        /// <summary>
        /// The maximum number of save-game descriptions that the list may hold.
        /// </summary>
        public const int MaximumSaveGameDescriptions = 5;


        /// <summary>
        /// XML serializer for SaveGameDescription objects.
        /// </summary>
        private static XmlSerializer saveGameDescriptionSerializer = 
            new XmlSerializer(typeof(SaveGameDescription));

        
        /// <summary>
        /// Refresh the list of save-game descriptions.
        /// </summary>
        public static void RefreshSaveGameDescriptions()
        {
            // clear the list
            saveGameDescriptions = null;

            // retrieve the storage device, asynchronously
            GetStorageDevice(RefreshSaveGameDescriptionsResult);
        }


        /// <summary>
        /// Asynchronous storage-device callback for 
        /// refreshing the save-game descriptions.
        /// </summary>
        private static void RefreshSaveGameDescriptionsResult(
            StorageDevice storageDevice)
        {
            // check the parameter
            if ((storageDevice == null) || !storageDevice.IsConnected)
            {
                return;
            }

            // open the container
            using (StorageContainer storageContainer =
                OpenContainer(storageDevice))
            {
                saveGameDescriptions = new List<SaveGameDescription>();
                // get the description list
                string[] filenames = 
                    storageContainer.GetFileNames("SaveGameDescription*.xml");
                // add each entry to the list
                foreach (string filename in filenames)
                {
                    SaveGameDescription saveGameDescription;

                    // check the size of the list
                    if (saveGameDescriptions.Count >= MaximumSaveGameDescriptions)
                    {
                        break;
                    }
                    // open the file stream
                    using (Stream fileStream = storageContainer.OpenFile(filename, FileMode.Open))
                    {
                        // deserialize the object
                        saveGameDescription =
                            saveGameDescriptionSerializer.Deserialize(fileStream)
                            as SaveGameDescription;
                        // if it's valid, add it to the list
                        if (saveGameDescription != null)
                        {
                            saveGameDescriptions.Add(saveGameDescription);
                        }
                    }
                }
            }
        }


        #endregion


        #region Storage


        /// <summary>
        /// The stored StorageDevice object.
        /// </summary>
        private static StorageDevice storageDevice;

        /// <summary>
        /// The container name used for save games.
        /// </summary>
        public static string SaveGameContainerName = "RolePlayingGame";


        /// <summary>
        /// A delegate for receiving StorageDevice objects.
        /// </summary>
        public delegate void StorageDeviceDelegate(StorageDevice storageDevice);

        /// <summary>
        /// Asynchronously retrieve a storage device.
        /// </summary>
        /// <param name="retrievalDelegate">
        /// The delegate called when the device is available.
        /// </param>
        /// <remarks>
        /// If there is a suitable cached storage device, 
        /// the delegate may be called directly by this function.
        /// </remarks>
        public static void GetStorageDevice(StorageDeviceDelegate retrievalDelegate)
        {
            // check the parameter
            if (retrievalDelegate == null)
            {
                throw new ArgumentNullException("retrievalDelegate");
            }

            // check the stored storage device
            if ((storageDevice != null) && storageDevice.IsConnected)
            {
                retrievalDelegate(storageDevice);
                return;
            }

            // the storage device must be retrieved
            //if (!Guide.IsVisible)
            //{
                // Reset the device
                storageDevice = null;
                StorageDevice.BeginShowSelector(GetStorageDeviceResult, retrievalDelegate);
            //}


        }


        /// <summary>
        /// Asynchronous callback to the guide's BeginShowStorageDeviceSelector call.
        /// </summary>
        /// <param name="result">The IAsyncResult object with the device.</param>
        private static void GetStorageDeviceResult(IAsyncResult result)
        {
            // check the parameter
            if ((result == null) || !result.IsCompleted)
            {
                return;
            }

            // retrieve and store the storage device
            storageDevice = StorageDevice.EndShowSelector(result);

            // check the new storage device 
            if ((storageDevice != null) && storageDevice.IsConnected)
            {
                // it passes; call the stored delegate
                StorageDeviceDelegate func = result.AsyncState as StorageDeviceDelegate;
                if (func != null)
                {
                    func(storageDevice);
                }
            }
        }

        /// <summary>
        /// Synchronously opens storage container
        /// </summary>
        private static StorageContainer OpenContainer(StorageDevice storageDevice)
        {
            IAsyncResult result =
                storageDevice.BeginOpenContainer(Session.SaveGameContainerName, null, null);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            StorageContainer container = storageDevice.EndOpenContainer(result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            return container;
        }


        #endregion

    
        #region Random


        /// <summary>
        /// The random-number generator used with game events.
        /// </summary>
        private static Random random = new Random();

        /// <summary>
        /// The random-number generator used with game events.
        /// </summary>
        public static Random Random
        {
            get { return random; }
        }


        #endregion
    }
}
