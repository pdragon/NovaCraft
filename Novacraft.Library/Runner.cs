using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Novacraft.Library.Authentication;
using Novacraft.Library.UsableClasses;
using Newtonsoft.Json;
using Serilog.Core;
using Spectre.Console;
using static Novacraft.Library.FilesManager;
using static Novacraft.Library.ForgeThingy;
using static Novacraft.Library.LauncherConfig;

namespace Novacraft.Library;

/// <summary>
/// Novacraft Runner
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
            /// A custom version located in .Novacraft/versions
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
    /// <param name="main">Novacraft Main JSON</param>
    /// <param name="config">Configuration</param>
    /// <returns>Generated command</returns>
    public static string GenerateCommand(LauncherConfig.ModPack modpack, NovacraftMainJson main, Configuration config)
    {
        AnsiConsole.WriteLine("[Runner] Generating command");
        var sb = new StringBuilder();

        var forgeVersionName = FilesManager.GetFirstForgeVersion(main.Version);
        //TODO: Add forge libraries path to -cp
        var forgeLibrariesList = FilesManager.GetForgeLibrariesPaths(modpack, forgeVersionName);
        if (forgeLibrariesList.Count != 0)
        {
            var tmpSourceList = main.Libraries.ToList();
            tmpSourceList.AddRange(forgeLibrariesList);
            main.Libraries = tmpSourceList.ToArray();
        }

        sb.Append(string.IsNullOrEmpty(config.JvmArgs)
            ? $"-Xms{config.RamMax}M -Xmx{config.RamMax}M "
            : $"-Xms{config.RamMax}M -Xmx{config.RamMax}M {config.JvmArgs} ");
        foreach (var arg in main.Arguments.Java)
        {
            var process = true;
            foreach (var str in arg.Disallow)
            {
                if (process == false) continue;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in arg.Allow)
            {
                if (process == false) continue;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            if (arg.ValueList.Length != 0)
            {
                foreach (var str in arg.ValueList) sb.Append($"{ReplaceJavaArguments(modpack, main, str, config)} ");
                continue;
            }

            sb.Append($"{ReplaceJavaArguments(modpack, main, arg.Value, config)} ");
        }

        sb.Append($"{main.MainClass} ");
        foreach (var arg in main.Arguments.Game)
        {
            var process = true;
            foreach (var str in arg.Disallow)
            {
                if (process == false) break;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in arg.Allow)
            {
                if (process == false) break;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            if (arg.ValueList.Length != 0)
            {
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
    /// <param name="main">Novacraft Main JSON</param>
    /// <param name="config">Configuration class</param>
    /// <returns>Classpath string</returns>
    private static string GenerateClasspath(LauncherConfig.ModPack modpack, NovacraftMainJson main, Configuration config)
    {
        var sb = new StringBuilder();
        //NovacraftMainJson.JsonLibrary[] jsonlibraries = new NovacraftMainJson.JsonLibrary[0];
        var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
        switch (config.Type)
        {
            case Configuration.VersionType.OfficialWithForgeModLoader:
                var forgeVersionName = FilesManager.GetFirstForgeVersion(main.Version);
                sb.Append($"\"{Path.Combine(FilesManager.Directories.VersionsRoot, forgeVersionName, $"{forgeVersionName}.jar")}{separator}");
                break;
            default:
                sb.Append($"\"{Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"{main.Version}.jar")}{separator}");
                break;
        }




        for (var index = 0; index < main.Libraries.Length; index++)
        {
            var lib = main.Libraries[index];
            if (lib.Extract) continue;
            switch (lib.Platform)
            {
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
            foreach (var str in lib.Disallow)
            {
                if (process == false) break;
                process = !CheckBool(config, str);
            }

            if (process == false) continue;
            foreach (var str in lib.Allow)
            {
                if (process == false) break;
                process = CheckBool(config, str);
            }

            if (process == false) continue;
            sb.Append(index == main.Libraries.Length - 1
                ? FilesManager.GetLibraryPath(modpack, lib)
                : $"{FilesManager.GetLibraryPath(modpack, lib)}{separator}");
        }
        sb.Append("\"");
        return sb.ToString();
    }

    /// <summary>
    /// Replaces arguments with required values
    /// </summary>
    /// <param name="main">Novacraft Main JSON</param>
    /// <param name="str">String</param>
    /// <param name="config">Configuration class</param>
    /// <returns>Processed string</returns>
    private static string ReplaceJavaArguments(LauncherConfig.ModPack modpack, NovacraftMainJson main, string str, Configuration config)
    {
        return str.Replace("${natives_directory}", Path.Combine(FilesManager.Directories.VersionsRoot,
                main.Version, "natives")).Replace("${launcher_name}", "Novacraft")
            .Replace("${launcher_version}", Assembly.GetExecutingAssembly().GetName().Version!.ToString())
            .Replace("${classpath}", GenerateClasspath(modpack, main, config))
            .Replace("${classpath_separator}", Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";")
            .Replace("${library_directory}", FilesManager.Directories.GetLibrariesRoot(modpack))
            .Replace("${version_name}", main.Version)
            .Replace("${user_properties}", "{}")
            //.Replace("${user_properties}", "{}")
            ;
    }

    /// <summary>
    /// Replaces arguments with required values
    /// </summary>
    /// <param name="main">Novacraft Main JSON</param>
    /// <param name="config">Configuration</param>
    /// <param name="str">String</param>
    /// <returns></returns>
    private static string ReplaceGameArguments(Configuration config, string str, NovacraftMainJson main)
    {
        var newstr = str.Replace("${clientid}", "minecraft");
        newstr = newstr.Replace("${auth_player_name}", config.Account.Name)
            .Replace("${assets_root}", FilesManager.Directories.AssetsRoot)
            .Replace("${game_directory}", FilesManager.Directories.Root)
            .Replace("${version_type}", main.Type.ToString().ToLower())
            .Replace("${assets_index_name}", main.Assets.Id).Replace("${version_name}", main.Version)
            // .Replace("${assets_index_name}", main.Version).Replace("${version_name}", main.Version)


            .Replace("${user_properties}", "{}");
        newstr = newstr.Replace("${user_type}", config.Account.Type.ToString().ToLower());
        switch (config.Account.Type)
        {
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
        switch (str)
        {
            case "is_demo_user": return config.DemoUser;
            case "has_custom_resolution": return config.CustomWindowSize;
            default:
                if (str.StartsWith("os-name:"))
                    switch (str.Substring(8))
                    {
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
    /// <param name="main">Novacraft Main JSON</param>
    /// <param name="addon">Novacraft Addon JSON</param>
    /// <param name="config">Configuration</param>
    /// <returns>Generated command</returns>
    public static string GenerateCommand(LauncherConfig.ModPack modpack, NovacraftMainJson main, NovacraftAddonJson addon, Configuration config)
    {
        AnsiConsole.WriteLine("[Runner] Novacraft Addon JSON is used");
        if (main.Version != addon.BaseVersion)
        {
            AnsiConsole.MarkupLine($"[red]Incompatible addon and main JSON files![/]");
            AnsiConsole.MarkupLine($"[red]Addon is for {addon.BaseVersion}, not for {main.Version}.[/]");
            Environment.Exit(-1);
        }
        var newlibs = main.Libraries.ToList();
        newlibs.AddRange(addon.Libraries);
        main.Libraries = newlibs.ToArray();
        main.MainClass = addon.MainClass;
        if (!addon.Legacy)
        {
            var gamelist = main.Arguments.Game.ToList();
            gamelist.AddRange(addon.Arguments.Game);
            main.Arguments.Game = gamelist.ToArray();
            var javalist = main.Arguments.Java.ToList();
            javalist.AddRange(addon.Arguments.Java);
            main.Arguments.Java = javalist.ToArray();
        }
        else main.Arguments.Game = addon.Arguments.Game;

        return GenerateCommand(modpack, main, config);
    }

    //public static void StartTheGame(NovacraftMainJson main, NovacraftAddonJson addonMain, Account account, bool online, LauncherConfig.ModPack modpack)
    public static void StartTheGame(LauncherGlobalProperties game, Action<string, string, string> progressBar)
    {
        var classpath = new StringBuilder();
        var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";

        //modpack.Time = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        var OsDict = new Dictionary<string, string>()
        {

        };

        static bool IsValidOs(string str)
        {
            if (str.StartsWith("os-name:"))
                switch (str.Substring(8))
                {
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
            return false;
        }

        //foreach (var library in game.MinecraftClientData.Libraries)
        for (var i = 0; i < game.MinecraftClientData.Libraries.Length; i++)
        {
            var library = game.MinecraftClientData.Libraries[i];
            if (game.AddonData != null)
                if (game.ModpackData.ModProxy.Equals("Forge") && game.AddonData.Libraries.Where(p => p.Name.Equals(library.Name)).Count() > 0)
                {
                    continue;
                }
            bool currentOs = false;
            foreach (var allowedOs in library.Allow)
            {
                currentOs = IsValidOs(allowedOs);
            }

            if (currentOs || library.Allow.Length == 0)
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && library.Allow.Contains("os-name:windows") || library.Allow.Length == 0)
            {
                var file2 = FilesManager.GetLibraryPath(game.ModpackData, new NovacraftMainJson.JsonLibrary
                {
                    Path = library.Path
                });
                if (!File.Exists(file2))
                {
                    if (game.ModpackData.ForceOffline)
                    {
                        return;
                    }
                    progressBar("Loading additional libraries", ((i / 100) * game.MinecraftClientData.Libraries.Length).ToString(), library.Name);
                    FilesManager.DownloadLibrary(library, game.ModpackData, game.Online);
                }
                classpath.Append($"{file2}{separator}");
            }
        }
        //classpath.Remove(classpath.Length - 1, 1);
        //TODO Add check for dir and file exist 
        //string file = Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"{main.Version}.jar");
        string file = Path.Combine(game.ModpackData.PackPath, game.MinecraftClientData.Version, $"{game.MinecraftClientData.Version}.jar");
        var args = new StringBuilder();

        string mainClass = game.MinecraftClientData.MainClass;
        classpath = AddonPostInstall(game, classpath, progressBar);
        if (game.ModpackData.ModProxy == "Forge")
        {
            mainClass = game.AddonData.MainClass;
        }

        if (game.ModpackData.CustomWindowSize)
        {
            args.Append($"-width {game.ModpackData.WindowSize.X} ");
            args.Append($"-height {game.ModpackData.WindowSize.Y} ");
        }
        else
        {
            args.Append($"-width {840} ");
            args.Append($"-height {480} ");
        }

        var args2 = new StringBuilder();
        // its works only in java 8
        List<string> JavaArguments = new List<string> { "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions", "-XX:+AlwaysPreTouch", "-XX:+ParallelRefProcEnabled", "-Xms${xms}M", "-Xmx${xmx}M", "-Dfile.encoding=UTF-8", "-Dfml.ignoreInvalidMinecraftCertificates=true", "-Dfml.ignorePatchDiscrepancies=true", "-Djava.net.useSystemProxies=true", "-Djava.library.path=${native_path}" };
        //TODO: java args pass
        foreach (var arg in JavaArguments)
        {
            var javaArgsReplaced = arg.Replace("${xms}", "512")
                    .Replace("${xmx}", game.ModpackData.RamMax)
                    //.Replace("${native_path}", Path.Combine(Directories.VersionsRoot, main.Version, "natives"));//"C:\\Users\\UserA\\AppData\\Roaming\\.Novacraft\\versions\\1.12.2\\natives");
                    .Replace("${native_path}", Path.Combine(CurentModPack.PackPath, game.MinecraftClientData.Version, "natives"));
            args2.Append($"{javaArgsReplaced} ");
        }

        if (game.AddonData != null && game.AddonData.Arguments != null && game.AddonData.Arguments.Java != null)
            foreach (var arg in game.AddonData.Arguments.Java)
            {
                var javaArgsReplaced = arg.Value.Replace("${version_name}", game.ModpackData.Version.Id)
                        //.Replace("${library_directory}", Path.Combine(Directories.Root, "libraries"))
                        .Replace("${library_directory}", Path.Combine(CurentModPack.PackPath, "libraries"))
                        .Replace("${classpath_separator}", separator);
                args2.Append($"{javaArgsReplaced} ");
            }

        var addonGameArgs = new StringBuilder();
        if (game.AddonData.Arguments != null && game.ModpackData.ModProxy == "Forge")
        {

            foreach (var arg in game.AddonData.Arguments.Game)
            {
                var replaced = arg.Value.Replace("${user_type}", "legacy")
                    //.Replace("${auth_access_token}", account.AccessToken)
                    //.Replace("${auth_uuid}", account.Uuid)
                    .Replace("${auth_access_token}", "0")
                    .Replace("${auth_uuid}", $"{game.AccountData.Uuid}")
                    .Replace("${assets_index_name}", game.MinecraftClientData.Assets.Id)
                    .Replace("${assets_root}", FilesManager.Directories.GetAssetsRoot(game.ModpackData))
                    .Replace("${game_directory}", game.ModpackData.PackPath) //FilesManager.Directories.Root)
                    .Replace("${version_name}", game.MinecraftClientData.Version)
                    .Replace("${auth_player_name}", game.AccountData.Name)
                    .Replace("${version_type}", "Novacraft")//"Novacraft")
                                                            // greater than 1.12.2 vesions
                    .Replace("${clientid}", "\"\"")
                    .Replace("${auth_xuid}", "\"\"")
                    .Replace("${user_properties}", "{}")
                    ;
                addonGameArgs.Append($" {replaced} ");
            }

            //TODO: add empty jat to libraries

        }
        if (!game.AddonData.Legacy)
        {
            foreach (var arg in game.MinecraftClientData.Arguments.Game)
            {
                var replaced = arg.Value.Replace("${user_type}", "legacy")
                    //.Replace("${auth_access_token}", account.AccessToken)
                    //.Replace("${auth_uuid}", account.Uuid)
                    .Replace("${auth_access_token}", "0")
                    .Replace("${auth_uuid}", $"{game.AccountData.Uuid}")
                    .Replace("${assets_index_name}", game.MinecraftClientData.Assets.Id)
                    .Replace("${assets_root}", FilesManager.Directories.GetAssetsRoot(game.ModpackData))
                    .Replace("${game_directory}", game.ModpackData.PackPath) //FilesManager.Directories.Root)
                    .Replace("${version_name}", game.MinecraftClientData.Version)
                    .Replace("${auth_player_name}", game.AccountData.Name)
                    .Replace("${version_type}", "modified")//"Novacraft")
                                                           // greater than 1.12.2 vesions
                    .Replace("${clientid}", "\"\"")
                    .Replace("${auth_xuid}", "\"\"")
                    //.Replace("--demo", "")
                    .Replace("${user_properties}", "{}")

                    ;
                if (!game.ModpackData.DemoUser)
                {
                    replaced = replaced.Replace("--demo", "");
                }
                args.Append($"{replaced} ");
            }
        }
        args.Remove(args.Length - 1, 1);

        var command = $"{args2} -cp {file}{separator}{classpath} {mainClass} {args} {addonGameArgs}";

        if (game.MinecraftClientData == null)
        {
            throw new ArgumentNullException(nameof(game.MinecraftClientData));
        }
        var process = new Process();
        // JAVA_HOME and PATH sets here 
        //System.Environment.SetEnvironmentVariable("Path", Path.Combine(Directories.JavaRoot, "8", "bin") + ";" + envPath, EnvironmentVariableTarget.User);
        //System.Environment.SetEnvironmentVariable("JAVA_HOME", Path.Combine(Directories.JavaRoot, "8"), EnvironmentVariableTarget.User);
        //TODO: Check paths for exists
        //var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
        //string[] envArr = System.Environment.GetEnvironmentVariable("Path").Split(separator);

        //Library.Common Env = new Library.Common();
        //Env.SetNovacraftEnvToPathEnv(Path.Combine(Directories.GetJavaRoot(game.ModpackData), "8", "bin"));

        //System.Environment.SetEnvironmentVariable("Path", Path.Combine(Directories.GetJavaRoot(game.ModpackData), "8", "bin") + ";" + envPath, EnvironmentVariableTarget.User);        
        //System.Environment.SetEnvironmentVariable("JAVA_HOME", Path.Combine(Directories.GetJavaRoot(game.ModpackData), "8"), EnvironmentVariableTarget.User);
        process.StartInfo = new ProcessStartInfo
        {
            //EnvironmentVariables["RAYPATH"] = "test",
            WorkingDirectory = FilesManager.Directories.Root,
            //FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot,
            FileName = Path.Combine(Path.Combine(FilesManager.Directories.GetJavaRoot(game.ModpackData),
                game.MinecraftClientData.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw"),
            //RedirectStandardOutput = true,
            //RedirectStandardError = true,
            UseShellExecute = true,
            Arguments = command
        };

        // TODO: console in MainTab
        //string std = process.StandardOutput.ReadToEnd();
        //string startFileContent = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot, game.MinecraftClientData.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw") + " " + command;
        string startFileContent = Path.Combine(Path.Combine(FilesManager.Directories.GetJavaRoot(game.ModpackData), game.MinecraftClientData.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw") + " " + command;
        string startFilePath = Path.Combine(game.ModpackData.PackPath, game.ModpackData.Version.Id + "_" + game.ModpackData.ModProxy + "." + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh"));
        if (!File.Exists(startFilePath))
        {
            File.WriteAllText(startFilePath, startFileContent);
        }

        //Console.WriteLine(Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot,
        Console.WriteLine(Path.Combine(Path.Combine(FilesManager.Directories.GetJavaRoot(game.ModpackData),
                game.MinecraftClientData.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw") + " " + command);
        progressBar("processing", "", "Game started, enjoy :-)");
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            AnsiConsole.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            AnsiConsole.WriteLine(e.Data);
        };
        process.Start();
        process.WaitForExit();
        progressBar("", "", "");
        // Secure errror on start
        // https://github.com/OpenFeign/feign/issues/935#issuecomment-521236281
    }

    private static StringBuilder AddonPostInstall(LauncherGlobalProperties game, StringBuilder classpath, Action<string, string, string> progressBar)
    {
        string mainClass = game.MinecraftClientData.MainClass;
        var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
        switch (game.ModpackData.ModProxy)
        {
            case "Forge":
                if (game.AddonData != null)
                {
                    mainClass = game.AddonData.MainClass;
                    if(!game.ModpackData.ForceOffline)
                    {
                        progressBar("Downloading forge libraries", "", "Please wait while installing");
                        //foreach (var library in game.AddonData.Libraries)
                        for (int i = 0; i < game.AddonData.Libraries.Length; i++)
                        {
                            var library = game.AddonData.Libraries[i];
                            var file2 = FilesManager.GetLibraryPath(game.ModpackData, new NovacraftMainJson.JsonLibrary
                            {
                                Path = library.Path
                            });
                            var hash = HashHelper.Hash(file2);

                            if ((game.ModpackData.ModProxyVersion == null || !game.ModpackData.ModProxyVersion.Installed) || hash != library.ShaHash)
                            {
                                var percent = (int)((float)((int)i + 1) / game.AddonData.Libraries.Length * 100);
                                progressBar(library.Name, (i + 1) + " in " + game.AddonData.Libraries.Length + "(" + percent + " %)", "Downloading forge libraries");
                                FilesManager.DownloadLibrary(library, game.ModpackData, game.Online);
                            }
                            classpath.Append($"{file2}{separator}");
                        }

                        //if (ForgeThingy.IsProcessorsExists(main.Version) && !ForgeThingy.ForgeIsInstalled())
                        if (!game.MinecraftClientData.Legacy && !game.ModpackData.ModProxyVersion.Installed)
                        {
                            progressBar("Please, be patient, it may spent some time", "", "Processing forge libraries");
                            ForgeThingy.RunProcessors(game.ModpackData, game.MinecraftClientData, game.Online);
                        }
                    }

                    progressBar("", "", "");
                    //TODO Add check for dir and file exist
                    //string addonFile = Path.Combine(FilesManager.Directories.Root, "forge", $"{addonMain.FullVersion}.jar");
                    if (game.MinecraftClientData.Legacy)
                    {
                        string addonFile = Path.Combine(game.ModpackData.PackPath, "forge", $"{game.AddonData.FullVersion}.jar");
                        //string addonFile = Path.Combine(modpack.PackPath, "forge", $"{modpack.ModProxyVersion.Name}.jar");
                        classpath.Append($"{addonFile}{separator}");
                        //return new AddonPostInstallReturn { AddonFilePath = addonFile, Classpath = classpath };
                        
                    }
                    
                }
                break;
            default:

                break;
        }
        return classpath;
        //return new AddonPostInstallReturn { Classpath = classpath };
    }
}