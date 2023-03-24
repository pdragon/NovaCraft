using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Blowaunch.Library;

/// <summary>
/// Blowaunch - Assets JSON
/// </summary>
public class BlowaunchAssetsJson
{
    /// <summary>
    /// Converts Mojang -> Blowaunch
    /// </summary>
    /// <param name="mojang"></param>
    /// <returns></returns>
    public static BlowaunchAssetsJson MojangToBlowaunch(MojangAssetsJson mojang)
    {
        var json = new BlowaunchAssetsJson {
            Author = "Mojang Studios",
            Information = "Mojang Assets JSON made to work with Blowaunch"
        };
        json.Assets = mojang.Assets.Select(pair => new JsonAsset {
            Name = pair.Key, 
            ShaHash = pair.Value.ShaHash, 
            Size = pair.Value.Size, 
            Url = new StringBuilder()
                .AppendFormat(Fetcher.MojangEndpoints.Asset, 
                    pair.Value.ShaHash.Substring(0, 2), pair.Value.ShaHash).ToString()
        }).ToArray();
        return json;
    }
        
    /// <summary>
    /// Blowaunch Assets JSON - Asset
    /// </summary>
    public class JsonAsset
    {
        [JsonProperty("sha1")] public string ShaHash;
        [JsonProperty("size")] public int Size;
        [JsonProperty("url")] public string Url;
        [JsonProperty("name")] public string Name;
    }
        
    [JsonProperty("author")] public string Author;
    [JsonProperty("info")] public string Information;
    [JsonProperty("assets")] public JsonAsset[] Assets;
}