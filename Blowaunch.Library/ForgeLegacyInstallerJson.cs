using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Blowaunch.Library.BlowaunchMainJson;
//using static Blowaunch.Library.ForgeInstallerJson;

namespace Blowaunch.Library
{
    public class ForgeLegacyInstallerJson
    {
        [JsonProperty("install")] public JsonInstall Install;
        [JsonProperty("versionInfo")] public VersionInfoType VersionInfo;
        

        public class JsonInstall {
            [JsonProperty("profileName")] public string ProfileName;
            [JsonProperty("target")] public string Target;
            [JsonProperty("path")] public string Path;
            [JsonProperty("version")] public string Version;
            [JsonProperty("filePath")] public string FilePath;
            [JsonProperty("welcome")] public string Welcome;
            [JsonProperty("minecraft")] public string Minecraft;
            [JsonProperty("mirrorList")] public string MirrorList;
            [JsonProperty("logo")] public string Logo;
        }

        public class VersionInfoType
        {
            [JsonProperty("id")] public string Id;
            [JsonProperty("time")] public string Time;
            [JsonProperty("releaseTime")] public string ReleaseTime;
            [JsonProperty("type")] public string Type;
            [JsonProperty("minecraftArguments")] public string MinecraftArguments;
            [JsonProperty("mainClass")] public string MainClass;
            [JsonProperty("minimumLauncherVersion")] public short MinimumLauncherVersion;
            [JsonProperty("assets")] public string Assets;
            [JsonProperty("inheritsFrom")] public string InheritsFrom;
            [JsonProperty("jar")] public string Jar;
            [JsonProperty("libraries")] public List<Library> Libraries;
        }

        public class Library
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("url")] public string Url;
            [JsonProperty("checksums")] public List<string> Checksums;
            [JsonProperty("serverreq")] public bool Serverreq;
            [JsonProperty("clientreq")] public bool Clientreq;
        }

        public static BlowaunchMainJson ForgeToBlowaunchPartial(ForgeLegacyInstallerJson mojang)
        {
            var json = new BlowaunchMainJson
            {
                MainClass = mojang.VersionInfo.MainClass,
                Author = "LexManos",
                Information = "Forge Legacy JSON made to work with Blowaunch",
                Arguments = new JsonArguments
                {
                    Game = new[] { new JsonArgument {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    ValueList = Array.Empty<string>(),
                    Value = mojang.VersionInfo.MinecraftArguments
                }}
                },
                Version = mojang.Install.Minecraft,
                Legacy = true
            };

            ProcessLegacyLibraries(mojang, out var libraries);
            json.Libraries = libraries.ToArray();
            return json;
        }
        
        public static void ProcessLegacyLibraries(ForgeLegacyInstallerJson forgeInstaller, out List<JsonLibrary> libraries)
        {
            
            libraries = new List<JsonLibrary>();
            foreach (var lib in forgeInstaller.VersionInfo.Libraries)
            {
                //if (lib.Clientreq)
                //{
                var split = lib.Name.Split(':');
                lib.Url = string.IsNullOrEmpty(lib.Url) ? "https://libraries.minecraft.net" : lib.Url;
                string postfix = split[0] == "net.minecraftforge" ? "-universal" : "";
                var main = new JsonLibrary
                {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    //Path = lib.Downloads.Artifact.Path,
                    //Path = lib.Downloads.Artifact == null ? null : lib.Downloads.Artifact.Path,
                    Path = Path.Combine(split[0].Replace(".", "/"), split[1], split[2], split[1] + "-" + split[2] + ".jar"),
                    Exclude = Array.Empty<string>(),
                    //Url = lib.Clientreq  ? string.Join("/", lib.Url, split[0].Replace(".", "/"), split[1], split[2], split[1] + "-" + split[2] + ".jar") : null,
                    Url = string.Join("/", lib.Url, split[0].Replace(".", "/"), split[1], split[2], split[1] + "-" + split[2] + $"{postfix}.jar"),
                    ShaHash = lib.Checksums != null && lib.Checksums.Count() > 0 ? lib.Checksums[0]: null,
                    Extract = false
                };
                libraries.Add(main);
                //}
            }
        }
        
    }
}
