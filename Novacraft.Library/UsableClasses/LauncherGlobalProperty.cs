using Novacraft.Library.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novacraft.Library.UsableClasses
{
    public class LauncherGlobalProperties
    {
        public LauncherConfig.ModPack ModpackData { get; set; }
        public NovacraftMainJson MinecraftClientData { get; set; }
        public NovacraftAddonJson AddonData { get; set; }
        //public Runner.Configuration RunnerConfig { get; set; }
        public Account AccountData { get; set; }
        public bool Online { get; set; }
    }
}
