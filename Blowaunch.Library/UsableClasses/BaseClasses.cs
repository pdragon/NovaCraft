using static Blowaunch.Library.UsableClasses.ShareModPack.ExportFileParams;

namespace Blowaunch.Library.UsableClasses
{
    /*
    public class AddonPostInstallReturn
    {
        public StringBuilder Classpath { get; set; }
        public string AddonFilePath { get; set; }
    }
    */
    public class ModPackShareInfoTransfer
    {
        public ShareAccount ShareModPackAccount {  get; set; }
        public LauncherConfig.ModPack Modpack { get; set; }
    }

    public class Pair
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; }
    }

    public class NamedPair
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; }
    }
}
