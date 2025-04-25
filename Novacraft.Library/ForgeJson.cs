using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using static Novacraft.Library.NovacraftMainJson;

namespace Novacraft.Library;

public class ForgeJson
{
    public enum Type
    {
        None,
        PreLegacy,
        Legacy,
        Ordinary
            // Modern,
            // PstModern
    }

    public static bool IsForgeJSONFilename(string filename)
    {
        return filename.Contains("-forge-");
    }

    public static Type GetType(string json)
    {
        if (json == null)
        {
            return Type.None;
        }
        var preLegacy = JsonConvert.DeserializeObject<ForgeLegacyInstallerJson>(json);
        if(preLegacy.Install != null)
        {
            return Type.PreLegacy;
        }
        var legacy = JsonConvert.DeserializeObject<MojangLegacyMainJson>(json);
        if(legacy?.Arguments != null)
        {
            return Type.Legacy;
        }

        var ordinary = JsonConvert.DeserializeObject<MojangMainJson>(json);
        if(legacy?.Arguments == null)
        {
            return Type.Ordinary;
        }
        return Type.None;
    }
}