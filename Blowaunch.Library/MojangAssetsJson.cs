using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Blowaunch.Library;

/// <summary>
/// Mojang - Assets JSON
/// </summary>
public class MojangAssetsJson
{
    
    /// <summary>
    /// Mojang Assets JSON - Asset
    /// </summary>
    public class JsonAsset
    {
        [JsonProperty("hash")] public string ShaHash;
        [JsonProperty("size")] public int Size;
    }
        
    [JsonProperty("objects")] public Dictionary<string, JsonAsset> Assets;

    /// <summary>
    /// Is a JSON a mojang one?
    /// </summary>
    /// <param name="json">Dynamic JSON</param>
    /// <returns>Boolean value</returns>
    public static bool IsMojangAssetsJson(dynamic json)
        => !Helper.HasProperty(json, "objects")
            || string.IsNullOrEmpty(json.objects);
}