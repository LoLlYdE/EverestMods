using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using MonoMod.Detour;

namespace Celeste.Mod.LevelRandomizer
{
    public class LevelRandomizer : EverestModule {

        public static LevelRandomizer Instance;

        private static string logfile = "TransitionLog.txt";

        public override Type SettingsType => typeof(LevelRandomizerSettings);

        LevelRandomizerSettings Settings => (LevelRandomizerSettings)Instance._Settings;

        private readonly static MethodInfo m_TransitionTo = typeof(Level).GetMethod("TransitionTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public delegate void d_TransitionTo(Level self, LevelData next, Vector2 direction);

        public static d_TransitionTo orig_TransitionTo;

        private class Transition {
            public Vector2 vector;
            public LevelData data;

            public Transition(Vector2 nextVector, LevelData nextData) {
                vector = nextVector;
                data = nextData;
            }
        }

        public static void TransitionTo(Level self, LevelData next, Vector2 direction) {
            Player player = self.Entities.FindFirst<Player>();
            //LogTransition(next, direction);
            LevelData data = getEnd(self);
            //Vector2 newPos = getNewPos(self, data);
            Vector2 newPlayerPos = PredictNewPlayerPos(next, data, player);
            Console.WriteLine();
            Console.WriteLine("predicted newPos:");
            Console.WriteLine("X: " + newPlayerPos.X + "Y: " + newPlayerPos.Y);
            SetPlayerPosition(self, newPlayerPos);
            orig_TransitionTo(self, data, direction);
        }


        public override void Load() {
            Type t_LevelRandomizer = GetType();
            orig_TransitionTo = m_TransitionTo.Detour<d_TransitionTo>(t_LevelRandomizer.GetMethod("TransitionTo"));
            Everest.Events.Level.OnPause += MovePlayer;

        }

        public override void Unload() {
            RuntimeDetour.Undetour(m_TransitionTo);
            Everest.Events.Level.OnPause -= MovePlayer;
        }

        public void MovePlayer(Level level, int i, bool a, bool b) {
            Player player = level.Entities.FindFirst<Player>();
            Console.WriteLine("X: " + player.X + " Y: " + player.Y);
        }

        private static LevelData getEnd(Level level) {
            foreach (LevelData data in level.Session.MapData.Levels) {
                if (data.Name == "end_6")
                    return data;
            }
            return null;
        }

        public static void Randomize(LevelData next, Vector2 direction) {

        }

        public static void LogTransition(LevelData next, Vector2 direction, string comment = "") {
            List<string> lines = new List<string> { };
            lines.Add(comment);
            lines.Add("direction.X = " + direction.X);
            lines.Add("direction.Y = " + direction.Y);
            lines.Add("next.Name = " + next.Name);
            lines.Add(Environment.NewLine);
            File.AppendAllLines(logfile, lines.AsEnumerable());
        }

        public static Vector2 PredictNewPlayerPos(LevelData levelData, LevelData levelData2, Player player) {
            int delta = levelData2.Bounds.Left - levelData.Bounds.Left;
            Vector2 currentPos = player.Position;
            Vector2 position = new Vector2(currentPos.X + delta, currentPos.Y);
            return position;
            
        }

        public static void SetPlayerPosition(Level level, Vector2 position) {
            Player player = level.Entities.FindFirst<Player>();
            player.Position = position;
            Console.WriteLine("player:");
            Console.WriteLine("X: " + player.X + " Y: " + player.Y);
            Console.WriteLine("CameraOFfset");
            Console.WriteLine("X: " + level.Session.LevelData.CameraOffset.X + " Y: " + level.Session.LevelData.CameraOffset.Y);
        }

        public static Vector2 getNewPos(Level level, LevelData newPos) {
            LevelData oldPos = level.Session.LevelData;
            Vector2 direction;
            direction = newPos.CameraOffset - oldPos.CameraOffset;

            return direction;
        }
    }
}
