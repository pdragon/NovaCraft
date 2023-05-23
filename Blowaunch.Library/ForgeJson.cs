using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Blowaunch.Library;

public class ForgeJson
{
    [JsonProperty("versions")] public Dictionary<string, string> Versions;

    public static bool IsForgeJSONFilename(string filename)
    {
        return filename.Contains("-forge-");
    }
}