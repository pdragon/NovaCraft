using System;
using System.Linq;
using Novacraft.Library.FetcherJson;
using Newtonsoft.Json;

namespace Novacraft.Library;

/// <summary>
/// Mojang Fetcher
/// </summary>
public static class MojangFetcher
{
    /// <summary>
    /// Get Mojang Versions JSON
    /// </summary>
    /// <returns>Mojang Versions JSON</returns>
    public static MojangVersionsJson GetVersions() 
        => JsonConvert.DeserializeObject<MojangVersionsJson>(Fetcher.Fetch(Fetcher.MojangEndpoints.Versions));
        
    /// <summary>
    /// Get Main JSON for Mojang Version
    /// </summary>
    /// <param name="ver">Version</param>
    /// <returns>Main JSON</returns>
    /// <exception cref="Exception">Unknown version</exception>
    public static NovacraftMainJson GetMain(string ver)
    {
        var versions = GetVersions();
        var versionFetch = versions.Versions.FirstOrDefault(x => x.Id == ver);
        if (versionFetch == null)
            throw new Exception("Unknown version!");
        var content = Fetcher.Fetch(versionFetch.Url);
        if (!MojangLegacyMainJson.IsLegacyJson(content)) {
            var versionMojang = JsonConvert.DeserializeObject<MojangMainJson>(content);
            return NovacraftMainJson.MojangToNovacraft(versionMojang);
        } else {
            var versionMojang = JsonConvert.DeserializeObject<MojangLegacyMainJson>(content);
            return NovacraftMainJson.MojangToNovacraft(versionMojang);
        }
    }
}