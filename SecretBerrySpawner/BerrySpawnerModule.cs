using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Celeste;
using Celeste.Mod;
using MonoMod;
using Monocle;
using Microsoft.Xna.Framework;
using FMOD.Studio;


namespace Celeste.Mod.SecretBerrySpawner
{
    public class BerrySpawnerModule : EverestModule {

        public static BerrySpawnerModule Instance;

        public static bool Enabled = false;

        public override Type SettingsType => typeof(BerrySpawnerModuleSettings);

        public static BerrySpawnerModuleSettings Settings => (BerrySpawnerModuleSettings)Instance._Settings;

        public override void Load() {
            Everest.Events.Level.OnPause += DoTheThing;
        }

        public override void Unload() {
            Everest.Events.Level.OnPause -= DoTheThing;
        }

        public BerrySpawnerModule() {
            Instance = this;
        }

        private void DumpInfo(Level level, int startIndex, bool minimal, bool quickReset) {
            List<string> lines = new List<string> { };
            lines.Add("level.Session.LevelData.Name = " + level.Session.LevelData.Name);
            lines.Add("level.Session.StartCheckpoint = " + level.Session.StartCheckpoint);
            lines.Add("level.Session.Area.ID = " + level.Session.Area.ID);
            lines.Add("level.Session.MapData.Filename = " + level.Session.MapData.Filename);
            File.WriteAllLines("LevelDataDump.txt", lines.ToArray());

        }



        public void DoTheThing(Level level, int startIndex, bool minimal, bool quickReset) {
            if (!Enabled)
                return;
            else
                Enabled = false;
            bool correctLevel = CheckLevel(level);
            if (correctLevel)
                SpawnBerry(level);
        }

        private bool CheckLevel(Level level) {
            return (level.Session.LevelData.Name == "end") && (level.Session.MapData.Filename == "1-ForsakenCity");
        }

        private void SpawnBerry(Level level) {
            int berryID = 4;
            Vector2 vector = new Vector2((float)level.Session.LevelData.Bounds.Left, (float)level.Session.LevelData.Bounds.Top);
            EntityData entityData = FindCorrectEntityData(level, berryID);
            EntityID entityID = new EntityID(level.Session.LevelData.Name, berryID);
            if (vector == null || entityData == null)
                return;
            Strawberry berry = new Strawberry(entityData, vector, entityID);
            bool berryExists = CheckBerryExists(level, berry.Position);
            if (!berryExists) {
                level.Add(berry);
            }
        }

        private EntityData FindCorrectEntityData(Level level, int ID) {
            foreach (EntityData entityData in level.Session.LevelData.Entities)
                if (entityData.ID == ID)
                    return entityData;
            return null;
        }

        private bool CheckBerryExists(Level level, Vector2 position) {
            foreach(Entity entity in level.Entities) {
                if (entity.Position == position)
                    return true;
            }
            return false;
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            menu.Add(new TextMenu.Button(Dialog.Clean("MODOPTIONS_BERRYSPAWNER_ENABLED")).Pressed(() => {
                Enabled = true;
            }));
        }
    }
}
