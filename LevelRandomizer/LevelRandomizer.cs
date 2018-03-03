using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using MonoMod.Detour;


namespace Celeste.Mod.LevelRandomizer{

    public class LevelRandomizer : EverestModule {

        #region vars

        public static LevelRandomizer Instance;

        private static string logfile = "TransitionLog.txt";

        private static List<TransitionMetadata> transitionMetadata;
        
        public override Type SettingsType => typeof(LevelRandomizerSettings);   
        static LevelRandomizerSettings Settings => (LevelRandomizerSettings)Instance._Settings;

        private readonly static MethodInfo m_TransitionTo = typeof(Level).GetMethod("TransitionTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private readonly static MethodInfo m_LoadLevel = typeof(Level).GetMethod("LoadLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public delegate void d_TransitionTo(Level self, LevelData next, Vector2 direction);

        public delegate void d_LoadLevel(Player.IntroTypes playerIntro, bool isFromLoader);

        public static d_TransitionTo orig_TransitionTo;

        public static d_LoadLevel orig_LoadLevel;

        #endregion

        public LevelRandomizer() {
            Instance = this;
        }

        public override void Load() {

            // detours
            Type t_LevelRandomizer = GetType();
            orig_TransitionTo = m_TransitionTo.Detour<d_TransitionTo>(t_LevelRandomizer.GetMethod("TransitionTo"));
            orig_LoadLevel = m_LoadLevel.Detour<d_LoadLevel>(t_LevelRandomizer.GetMethod("LoadLevel"));

            // Events
            Everest.Events.Level.OnEnter += LoadLevels;

            // TODO do I even need this?
            LoadInternalResources();
        }

        public override void Unload() {
            RuntimeDetour.Undetour(m_TransitionTo);
            RuntimeDetour.Undetour(m_LoadLevel);
            Everest.Events.Level.OnEnter -= LoadLevels;
        }

        public static void TransitionTo(Level self, LevelData next, Vector2 direction) {
            if (Settings.Enabled) {
                Player player = self.Entities.FindFirst<Player>();
                Debug.LogTransition(next, direction, player);
                TransitionMetadata metadata = getRandomTransitionData();
                LevelData newNext = getLevelDataByName(metadata.nextName, self.Session);
                int offsetX, offsetY;
                offsetX = metadata.directionX * 10;
                offsetY = metadata.directionY * 10;
                if (metadata.directionY < 0)
                    offsetY *= 3;
                player.Speed = Vector2.Zero;
                Vector2 newPlayerPos = new Vector2(metadata.playerX + offsetX, metadata.playerY + offsetY);
                Vector2 newDirection = new Vector2(metadata.directionX, metadata.directionY);
                SetPlayerPosition(self, newPlayerPos);
                orig_TransitionTo(self, newNext, newDirection);
            }
            else {
                orig_TransitionTo(self, next, direction);
            }
        }

        public static void LoadLevel(Player.IntroTypes playerIntro, bool isFromLoader = false) {
            if (Settings.Enabled) {
                if (playerIntro == Player.IntroTypes.Transition)
                    playerIntro = Player.IntroTypes.Respawn;
            }
            orig_LoadLevel(playerIntro, isFromLoader);
        }

        private static LevelData getLevelDataByName(string name, Session session) {
            LevelData toReturn = null;

            foreach (LevelData ld in session.MapData.Levels) {
                if (ld.Name == name)
                    toReturn = ld;
            }

            return toReturn;
        }

        protected static TransitionMetadata getRandomTransitionData() {
            int num = transitionMetadata.Count();
            var rng = new Random();
            int at = rng.Next(0, num);
            TransitionMetadata toReturn = transitionMetadata[at];
            transitionMetadata.RemoveAt(at);
            return toReturn;
        }

        private void LoadInternalResources() {
        }

        public static void LoadLevels(Session session, bool fromSaveData) {
            ModAsset metadata = Everest.Content.Get("Transitions/" + session.MapData.Filename);
            if (metadata == null) {
                Logger.Log("LevelRandomizer", session.MapData.Filename + " not found. Check your mod installation or contact the mod author");
            }
            transitionMetadata = new List<TransitionMetadata>(metadata.Deserialize<List<TransitionMetadata>>());
        }

        public static void SetPlayerPosition(Level level, Vector2 position) {
            Player player = level.Entities.FindFirst<Player>();
            player.Position = position;
            Console.WriteLine("player:");
            Console.WriteLine("X: " + player.X + " Y: " + player.Y);
            Console.WriteLine("CameraOffset");
            Console.WriteLine("X: " + level.Session.LevelData.CameraOffset.X + " Y: " + level.Session.LevelData.CameraOffset.Y);
        }

        protected class Transition {
            public Vector2 vector;
            public LevelData data;

            public Transition(Vector2 nextVector, LevelData nextData) {
                vector = nextVector;
                data = nextData;
            }
        }

        protected class TransitionMetadata {
            public int playerX { get; set; }
            public int playerY { get; set; }
            public int directionX { get; set; }
            public int directionY { get; set; }
            public string nextName { get; set; }
        }

        protected static class Debug {
            public static void LogTransition(LevelData next, Vector2 direction, Player player, string comment = "") {
                List<string> lines = new List<string> { };
                lines.Add(comment);
                lines.Add("player.X = " + player.X);
                lines.Add("player.Y = " + player.Y);
                lines.Add("direction.X = " + direction.X);
                lines.Add("direction.Y = " + direction.Y);
                lines.Add("next.Name = " + next.Name);
                lines.Add(Environment.NewLine);
                File.AppendAllLines(logfile, lines.AsEnumerable());
            }
        }
    }
}
