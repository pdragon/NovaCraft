using System;
using System.Text;
using Novacraft.Library.FetcherJson;
using Newtonsoft.Json;

namespace Novacraft.Library;

/// <summary>
/// Fabric Fetcher
/// </summary>
public static class FabricFetcher
{
    /// <summary>
    /// Fetch Fabric Addon JSON
    /// </summary>
    /// <param name="version">Mojang Version</param>
    /// <returns>Addon JSON</returns>
    public static NovacraftAddonJson GetAddon(string version)
    {
        var data = "{ \"Data\":" + Fetcher.Fetch(new StringBuilder().AppendFormat(
            Fetcher.FabricEndpoints.VersionLoaders, version).ToString()) + "}";
        var loaders = JsonConvert.DeserializeObject<FabricLoadersJson>(data);
        if (loaders == null)
            throw new Exception($"Unable to find Fabric Loader JSON for {version}!");
        return NovacraftAddonJson.MojangToNovacraft(JsonConvert.DeserializeObject<FabricJson>
        (Fetcher.Fetch(new StringBuilder().AppendFormat(Fetcher.FabricEndpoints.LoaderJson, 
            version, loaders.Data[0].Loader.Version).ToString())));
    }
}