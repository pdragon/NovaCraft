using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Blowaunch.Library.Authentication;
using Newtonsoft.Json;
using Serilog.Core;
using Spectre.Console;

namespace Blowaunch.Library;

/// <summary>
/// Blowaunch Runner
/// </summary>
public static class Runner
{
    /// <summary>
    /// Configuration class
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Version type
        /// </summary>
        public enum VersionType
        {
            /// <summary>
            /// Official Mojang version
            /// </summary>
            OfficialMojang = 0,
                
            /// <summary>
            /// A custom version located in .blowaunch/versions
            /// </summary>
            CustomVersionFromDir = 1,
                
            /// <summary>
            /// Custom version with an addon config file in the
            /// directory with the version and with name "addon.json"
            /// </summary>
            CustomWithAddonConfig = 2,
                
            /// <summary>
            /// Official Mojang version with an addon config file
            /// in the directory with the version and with name "addon.json"
            /// </summary>
            OfficialWithAddonConfig = 3,
                
            /// <summary>
            /// Official Mojang version with an addon config file
            /// that loads Forge Mod Loader, downloaded from GitHub repo
            /// </summary>
            OfficialWithForgeModLoader = 4,
                
            /// <summary>
            /// Official Mojang version with an addon config file
            /// that loads Fabric Mod Loader, downloaded from their API
            /// </summary>
            OfficialWithFabricModLoader = 5
        }

        [JsonProperty("maxRam")] public string RamMax = "";
        [JsonProperty("jvmArgs")] public string JvmArgs = "";
        [JsonProperty("gameArgs")] public string GameArgs = "";
        [JsonProperty("customResolution")] public bool CustomWindowSize;
        [JsonProperty("windowSize")] public Vector2 WindowSize = new(200, 200);
        [JsonProperty("version")] public string Version = "";
        [JsonProperty("type")] public VersionType Type = VersionType.OfficialMojang;
        [JsonProperty("forceOffline")] public bool ForceOffline;
        [JsonProperty("isDemo")] public bool DemoUser;
        [JsonProperty("auth")] public Account Account;
    }

    /// <summary>
    /// Generate run command
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="config">Configuration</param>
    /// <returns>Generated command</returns>
    public static string GenerateCommand(BlowaunchMainJson main, Configuration config)
    {
        AnsiConsole.WriteLine("[Runner] Generating command");
        var sb = new StringBuilder();
        sb.Append(string.IsNullOrEmpty(config.JvmArgs)
            ? $"-Xms{config.RamMax}M -Xmx{config.RamMax}M "
            : $"-Xms{config.RamMax}M -Xmx{config.RamMax}M {config.JvmArgs} ");
        foreach (var arg in main.Arguments.Java) {
            var process = true;
            foreach (var str in arg.Disallow) {
                if (process == false) continue;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in arg.Allow) {
                if (process == false) continue;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            if (arg.ValueList.Length != 0) {
                foreach (var str in arg.ValueList) sb.Append($"{ReplaceJavaArguments(main, str, config)} ");
                continue;
            }

            sb.Append($"{ReplaceJavaArguments(main, arg.Value, config)} ");
        }

        sb.Append($"{main.MainClass} ");
        foreach (var arg in main.Arguments.Game) {
            var process = true;
            foreach (var str in arg.Disallow) {
                if (process == false) break;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in arg.Allow) {
                if (process == false) break;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            if (arg.ValueList.Length != 0) {
                foreach (var str in arg.ValueList) sb.Append($"{ReplaceGameArguments(config, str, main)} ");
                continue;
            } 
                
            sb.Append($"{ReplaceGameArguments(config, arg.Value, main)} ");
        }

        AnsiConsole.WriteLine($"[Runner] Full command: {sb}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates classpath string
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="config">Configuration class</param>
    /// <returns>Classpath string</returns>
    private static string GenerateClasspath(BlowaunchMainJson main, Configuration config)
    {
        var sb = new StringBuilder();
        var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
        sb.Append($"\"{Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"{main.Version}.jar")}{separator}");
        for (var index = 0; index < main.Libraries.Length; index++) {
            var lib = main.Libraries[index];
            if (lib.Extract) continue;
            switch (lib.Platform) {
                case "windows":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        continue;
                    break;
                case "linux":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        continue;
                    break;
                case "macos":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
                        && Environment.OSVersion.Version.Major < 10 || 
                        Environment.OSVersion.Version.Minor < 12)
                        continue;
                    break;
                case "osx":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
                        && Environment.OSVersion.Version.Major >= 10 || 
                        Environment.OSVersion.Version.Minor >= 12)
                        continue;
                    break;
            }
            var process = true;
            foreach (var str in lib.Disallow) {
                if (process == false) break;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in lib.Allow) {
                if (process == false) break;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            sb.Append(index == main.Libraries.Length - 1
                ? FilesManager.GetLibraryPath(lib)
                : $"{FilesManager.GetLibraryPath(lib)}{separator}");
        }
        sb.Append("\"");
        return sb.ToString();
    }

    /// <summary>
    /// Replaces arguments with required values
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="str">String</param>
    /// <param name="config">Configuration class</param>
    /// <returns>Processed string</returns>
    private static string ReplaceJavaArguments(BlowaunchMainJson main, string str, Configuration config)
    {
        return str.Replace("${natives_directory}", Path.Combine(FilesManager.Directories.VersionsRoot,
                main.Version, "natives")).Replace("${launcher_name}", "Blowaunch")
            .Replace("${launcher_version}", Assembly.GetExecutingAssembly().GetName().Version!.ToString())
            .Replace("${classpath}", GenerateClasspath(main, config))
            .Replace("${classpath_separator}", Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";")
            .Replace("${library_directory}", FilesManager.Directories.LibrariesRoot)
            .Replace("${version_name}", main.Version)
            
            //.Replace("${user_properties}", "{}")
            ;
    }

    /// <summary>
    /// Replaces arguments with required values
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="config">Configuration</param>
    /// <param name="str">String</param>
    /// <returns></returns>
    private static string ReplaceGameArguments(Configuration config, string str, BlowaunchMainJson main)
    {
        var newstr = str.Replace("${clientid}", "minecraft");
        newstr = newstr.Replace("${auth_player_name}", config.Account.Name)
            .Replace("${assets_root}", FilesManager.Directories.AssetsRoot)
            .Replace("${game_directory}", FilesManager.Directories.Root)
            .Replace("${version_type}", main.Type.ToString().ToLower())
            .Replace("${assets_index_name}", main.Assets.Id).Replace("${version_name}", main.Version)

            .Replace("${user_properties}", "{}");
        newstr = newstr.Replace("${user_type}", config.Account.Type.ToString().ToLower());
        switch (config.Account.Type) {
            case Account.AuthType.Microsoft:
                newstr = newstr.Replace("${auth_access_token}", config.Account.AccessToken)
                    .Replace("${auth_xuid}", config.Account.Xuid).Replace("${auth_uuid}", 
                        config.Account.Uuid);
                break;
            case Account.AuthType.Mojang:
                newstr = newstr.Replace("${auth_access_token}", config.Account.AccessToken)
                    .Replace("${auth_xuid}", "noauth").Replace("${auth_uuid}", 
                        config.Account.Uuid);
                break;
            default:
                newstr = newstr.Replace("${clientid}", "noauth").Replace("${auth_access_token}", "noauth")
                    .Replace("${user_type}", "noauth").Replace("${auth_xuid}", "noauth")
                    .Replace("${auth_uuid}", "noauth");
                break;
        }

        return newstr;
    }

    /// <summary>
    /// Check Allow/Disallow value
    /// </summary>
    /// <param name="config">Configuration</param>
    /// <param name="str">String</param>
    /// <returns></returns>
    private static bool CheckBool(Configuration config, string str)
    {
        switch (str) {
            case "is_demo_user": return config.DemoUser;
            case "has_custom_resolution": return config.CustomWindowSize;
            default:
                if (str.StartsWith("os-name:"))
                    switch (str.Substring(8)) {
                        case "windows":
                            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                        case "linux":
                            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                        case "macos":
                            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                                   && Environment.OSVersion.Version.Major < 10 ||
                                   Environment.OSVersion.Version.Minor < 12;
                        case "osx":
                            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                                   && Environment.OSVersion.Version.Major >= 10 ||
                                   Environment.OSVersion.Version.Minor >= 12;
                    }
                if (str.StartsWith("os-version:"))
                    return new Regex(Environment.OSVersion.Version.ToString()).Matches(str.Substring(11)).Count != 0;
                return false;
        }
    }

    /// <summary>
    /// Generate run command
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="addon">Blowaunch Addon JSON</param>
    /// <param name="config">Configuration</param>
    /// <returns>Generated command</returns>
    public static string GenerateCommand(BlowaunchMainJson main, BlowaunchAddonJson addon, Configuration config) 
    {
        AnsiConsole.WriteLine("[Runner] Blowaunch Addon JSON is used");
        if (main.Version != addon.BaseVersion) {
            AnsiConsole.MarkupLine($"[red]Incompatible addon and main JSON files![/]");
            AnsiConsole.MarkupLine($"[red]Addon is for {addon.BaseVersion}, not for {main.Version}.[/]");
            Environment.Exit(-1);
        }
        var newlibs = main.Libraries.ToList();
        newlibs.AddRange(addon.Libraries);
        main.Libraries = newlibs.ToArray();
        main.MainClass = addon.MainClass;
        if (!addon.Legacy) {
            var gamelist = main.Arguments.Game.ToList();
            gamelist.AddRange(addon.Arguments.Game);
            main.Arguments.Game = gamelist.ToArray();
            var javalist = main.Arguments.Java.ToList();
            javalist.AddRange(addon.Arguments.Java);
            main.Arguments.Java = javalist.ToArray();
        } else main.Arguments.Game = addon.Arguments.Game;

        return GenerateCommand(main, config);
    }
}