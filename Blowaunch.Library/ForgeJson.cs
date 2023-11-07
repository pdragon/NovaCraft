using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Blowaunch.Library;

public class ForgeJson
{
    public static bool IsForgeJSONFilename(string filename)
    {
        return filename.Contains("-forge-");
    }
}