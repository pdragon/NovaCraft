using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Novacraft.Library;

/// <summary>
/// Novacraft - Addon JSON
/// </summary>
public class NovacraftAddonJson
{
    /// <summary>
    /// Processes NovacraftAddonJson Libraries
    /// </summary>
    /// <param name="json">Original instance</param>
    /// <returns>Processed instance</returns>
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    private static NovacraftAddonJson ProcessLibraries(NovacraftAddonJson json)
    {
        var toDelete = new List<NovacraftMainJson.JsonLibrary>();
        foreach (var lib in json.Libraries) {
            if (lib.Url == null) {
                toDelete.Add(lib);
                continue;
            }

            if (lib.Url.Contains("maven") && !lib.Url.EndsWith(".jar")) {
                lib.Url = lib.Platform == "any" ? $"{lib.Url}{string.Join("/", lib.Package.Split('.'))}/{lib.Name}/{lib.Version}/" +
                                                  $"{lib.Name}-{lib.Version}.jar"
                    : new StringBuilder().AppendFormat(Fetcher.MojangEndpoints.Library, string.Join("/", 
                        lib.Package.Split('.')), lib.Name, lib.Version, $"-natives-{lib.Platform}").ToString();
            }
                
            lib.ShaHash ??= Fetcher.Fetch($"{lib.Url}.sha1");
        }

        var result = json.Libraries.ToList();
        foreach (var i in toDelete) result.Remove(i);
        json.Libraries = result.ToArray();
        return json;
    }
        
    /// <summary>
    /// Converts Novacraft -> Mojang
    /// </summary>
    /// <param name="mojang">Mojang JSON</param>
    /// <returns>Novacraft JSON</returns>
    public static NovacraftAddonJson MojangToNovacraft(MojangMainJson mojang)
    {
        var json = new NovacraftAddonJson {
            Arguments = new NovacraftMainJson.JsonArguments(),
            MainClass = mojang.MainClass,
            Author = "Mojang Studios",
            Information = "Mojang JSON made to work with Novacraft",
            BaseVersion = mojang.Version
        };
            
        NovacraftMainJson.ProcessArguments(mojang, out var gameArguments, 
            out var jvmArguments);
        json.Arguments.Game = gameArguments.ToArray();
        json.Arguments.Java = jvmArguments.ToArray();
        NovacraftMainJson.ProcessLibraries(mojang, out var libraries);
        json.Libraries = libraries.ToArray();
        json = ProcessLibraries(json);
        return json;
    }
        
    /// <summary>
    /// Converts Novacraft -> Fabric
    /// </summary>
    /// <param name="fabric">Fabric JSON</param>
    /// <returns>Novacraft JSON</returns>
    public static NovacraftAddonJson MojangToNovacraft(FabricJson fabric)
    {
        var json = new NovacraftAddonJson {
            MainClass = fabric.MainClass,
            Author = "FabricMC Contributors",
            Information = "Fabric JSON made to work with Novacraft",
            BaseVersion = fabric.BaseVersion,
        };
            
        var libraries = new List<NovacraftMainJson.JsonLibrary>();
        foreach (var lib in fabric.Libraries) {
            var split = lib.Name.Split(':');
            var main = new NovacraftMainJson.JsonLibrary {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                Path = $"{split[0]}/{split[1]}/{split[2]}/{split[1]}-{split[2]}.jar",
                Package = split[0],
                Name = split[1],
                Version = split[2],
                Platform = "any",
                Url = lib.Url
            };
                
            libraries.Add(main);
        }

        var game = new List<NovacraftMainJson.JsonArgument>();
        var java = new List<NovacraftMainJson.JsonArgument>();
        foreach (var i in fabric.Arguments.Game)
            game.Add(new NovacraftMainJson.JsonArgument {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                ValueList = Array.Empty<string>(),
                Value = i.Replace(" ", "")
            });
        foreach (var i in fabric.Arguments.Java)
            java.Add(new NovacraftMainJson.JsonArgument {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                ValueList = Array.Empty<string>(),
                Value = i.Replace(" ", "")
            });
        json.Arguments = new NovacraftMainJson.JsonArguments {
            Game = game.ToArray(),
            Java = java.ToArray()
        };
        json.Libraries = libraries.ToArray();
        json = ProcessLibraries(json);
        return json;
    }
    /*
    /// <summary>
    /// Converts Forge -> Novacraft
    /// </summary>
    /// <param name="fabric">Fabric JSON</param>
    /// <returns>Novacraft JSON</returns>
    public static NovacraftAddonJson ForgeToNovacraft(ForgeJson forge)
    {
        var json = new NovacraftAddonJson
        {
            MainClass = fabric.MainClass,
            Author = "FabricMC Contributors",
            Information = "Fabric JSON made to work with Novacraft",
            BaseVersion = fabric.BaseVersion,
        };

        var libraries = new List<NovacraftMainJson.JsonLibrary>();
        foreach (var lib in fabric.Libraries)
        {
            var split = lib.Name.Split(':');
            var main = new NovacraftMainJson.JsonLibrary
            {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                Path = $"{split[0]}/{split[1]}/{split[2]}/{split[1]}-{split[2]}.jar",
                Package = split[0],
                Name = split[1],
                Version = split[2],
                Platform = "any",
                Url = lib.Url
            };

            libraries.Add(main);
        }

        var game = new List<NovacraftMainJson.JsonArgument>();
        var java = new List<NovacraftMainJson.JsonArgument>();
        foreach (var i in fabric.Arguments.Game)
            game.Add(new NovacraftMainJson.JsonArgument
            {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                ValueList = Array.Empty<string>(),
                Value = i.Replace(" ", "")
            });
        foreach (var i in fabric.Arguments.Java)
            java.Add(new NovacraftMainJson.JsonArgument
            {
                Allow = Array.Empty<string>(),
                Disallow = Array.Empty<string>(),
                ValueList = Array.Empty<string>(),
                Value = i.Replace(" ", "")
            });
        json.Arguments = new NovacraftMainJson.JsonArguments
        {
            Game = game.ToArray(),
            Java = java.ToArray()
        };
        json.Libraries = libraries.ToArray();
        json = ProcessLibraries(json);
        return json;
    }
    */
    [JsonProperty("legacy")] public bool Legacy;
    [JsonProperty("baseVersion")] public string BaseVersion;
    [JsonProperty("fullVersion")] public string FullVersion;
    [JsonProperty("author")] public string Author;
    [JsonProperty("info")] public string Information;
    [JsonProperty("libraries")] public NovacraftMainJson.JsonLibrary[] Libraries;
    [JsonProperty("args")] public NovacraftMainJson.JsonArguments Arguments;
    [JsonProperty("mainClass")] public string MainClass;
}