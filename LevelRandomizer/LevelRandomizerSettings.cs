using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.LevelRandomizer {
    [SettingName("Level Randomizer")]
    public class LevelRandomizerSettings : EverestModuleSettings{

        public bool Enabled { get; set; } = false;
    }
}
