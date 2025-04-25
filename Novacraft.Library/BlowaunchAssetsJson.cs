using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Novacraft.Library;

/// <summary>
/// Novacraft - Assets JSON
/// </summary>
public class NovacraftAssetsJson
{
    /// <summary>
    /// Converts Mojang -> Novacraft
    /// </summary>
    /// <param name="mojang"></param>
    /// <returns></returns>
    public static NovacraftAssetsJson MojangToNovacraft(MojangAssetsJson mojang)
    {
        var json = new NovacraftAssetsJson {
            Author = "Mojang Studios",
            Information = "Mojang Assets JSON made to work with Novacraft"
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
    /// Novacraft Assets JSON - Asset
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