using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blowaunch.Library;
using Blowaunch.Library.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
//using static System.Net.WebRequestMethods;
using static Blowaunch.Library.FilesManager;
using static Blowaunch.Library.ForgeThingy;
using static Blowaunch.Library.LauncherConfig;
//using File = System.IO.File;

namespace Blowaunch.Library
{
    /// <summary>
    /// Parse the website and get the installer link.
    /// After that we download it, extract and get
    /// everything we need.
    ///
    /// Sorry LexManos :)
    /// </summary>
    public static class ForgeThingy
    {
        public class Versions
        {
            public int? ComboboxItemId { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        async private static Task<bool> LinkIsBronen(string uri)
        {
            var httpClient = new HttpClient();
            //IHttpClientFactory httpFactory;
            //var we = new WebException();
            HttpResponseMessage message = new HttpResponseMessage();
            message = await httpClient.GetAsync(uri);
            return message.StatusCode != HttpStatusCode.OK;
        }
        
        /// <summary>
        /// Get the installer link
        /// </summary>
        /// <param name="version">Version</param>
        /// <returns>Installer link</returns>
        async public static Task<string> GetLink(string version)
        {
            try {
                var forgeLink = string.Format(Fetcher.ForgeEndpoints.ForgeWebsite, version);
                bool forgeIsbsent = await LinkIsBronen(forgeLink);
                if (!forgeIsbsent)
                {
                    var content = Fetcher.Fetch(forgeLink);
                    var recommended = content.IndexOf("Download Recommended", StringComparison.Ordinal);
                    var latest = content.IndexOf("Download Latest", StringComparison.Ordinal);
                    var subst = content.Substring(recommended == -1 ? latest : recommended);
                    var download = subst.IndexOf("<a href=\"", StringComparison.Ordinal);
                    var subst2 = subst.Substring(download);
                    var url = subst2.IndexOf("url=", StringComparison.Ordinal) + "url=".Length;
                    var subst3 = subst2.Substring(url);
                    var end = subst3.IndexOf("\"", StringComparison.Ordinal);
                    //return "https://maven.minecraftforge.net/net/minecraftforge/forge/1.19.2-43.3.2/forge-1.19.2-43.3.2-installer.jar";
                    return subst3.Substring(0, end);
                }
            } catch (Exception e) {
                AnsiConsole.MarkupLine("[red]Unable to parse the website: An exception occured.[/]");
                AnsiConsole.WriteException(e);
                Environment.Exit(-1);
                return null;
            }
            return null;
        }
        

        async public static Task<List<string>> GetVersions()
        {
            var forgeLink = "https://files.minecraftforge.net/net/minecraftforge/forge/";
            bool offline = await LinkIsBronen(forgeLink);
            List<string> mcVersions = new List<string>();
            if (!offline)
            {
                var content = Fetcher.Fetch(forgeLink);
                var panelStart = content.IndexOf("Minecraft Version", StringComparison.Ordinal);
                var panelEnd = content.IndexOf("</aside>", StringComparison.Ordinal);
                var panel = content.Substring(panelStart, panelEnd - panelStart);
                var panelList = panel.Split("<li>");
                foreach (string versionLabel in panelList)
                {
                    if (versionLabel.IndexOf("<a href=\"#\"", StringComparison.Ordinal) == -1)
                    {
                        var download = versionLabel.IndexOf("<a href=\"", StringComparison.Ordinal);
                        var subst2 = versionLabel.Substring(download);
                        var urlEnd = subst2.IndexOf(".html\">", StringComparison.Ordinal) + ".html\">".Length;
                        var linkEnd = subst2.IndexOf("</a>", StringComparison.Ordinal);
                        var version = subst2.Substring(urlEnd, linkEnd - urlEnd);
                        mcVersions.Add(version);
                    }
                }
                //mcVersions.AddRange(panelList);
            }
            return mcVersions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version">version of minecraft modpack, for examlpe: 1.19.2</param>
        /// <returns></returns>
        async public static Task<List<Versions>> GetLinks(string version)
        {
            try
            {
                var forgeLink = string.Format(Fetcher.ForgeEndpoints.ForgeWebsite, version);
                bool forgeIsbsent = await LinkIsBronen(forgeLink);
                List<Versions> forgeVersions = new List<Versions>();
                if (!forgeIsbsent)
                {
                    var content = Fetcher.Fetch(forgeLink);
                    var tableStart = content.IndexOf("download-list", StringComparison.Ordinal);
                    var tableEnd = content.IndexOf("</tbody>", StringComparison.Ordinal); 
                    var subst = content.Substring(tableStart, tableEnd - tableStart);
                    var table = subst;
                    var itemsAmount = Regex.Matches(content, "<tr>").Count;
                    var from = content.IndexOf("<tr>", StringComparison.Ordinal);
                    var split = table.Split("<tr>");
                    //for (int i = 0; i < split.Length; i++)
                    foreach(string downloadBlock in split)
                    {
                        var download = downloadBlock.IndexOf("<a href=\"", StringComparison.Ordinal);
                        // thead block
                        if (download == -1 || downloadBlock.IndexOf("-installer.jar", StringComparison.Ordinal) == -1)
                        {
                            continue;
                        }
                        
                        var subst2 = downloadBlock.Substring(download);
                        var url = subst2.IndexOf("url=", StringComparison.Ordinal) + "url=".Length;
                        var subst3 = subst2.Substring(url);
                        var end = subst3.IndexOf("\"", StringComparison.Ordinal);
                        var realUrl = subst3.Substring(0, end);
                        var forgePos = realUrl.IndexOf("forge-", StringComparison.Ordinal);
                        var name = realUrl.Substring(forgePos, realUrl.IndexOf("-installer.jar", StringComparison.Ordinal) - forgePos);
                        forgeVersions.Add(new Versions{ Url = realUrl, Name = name});
                    }
                    //return "https://maven.minecraftforge.net/net/minecraftforge/forge/1.19.2-43.3.2/forge-1.19.2-43.3.2-installer.jar";
                    return forgeVersions;
                }
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine("[red]Unable to parse the website: An exception occured.[/]");
                AnsiConsole.WriteException(e);
                Environment.Exit(-1);
                return null;
            }
            return null;
        }

        /// <summary>
        /// Get artifact's path
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="original">Return the original data if it is not in brackets</param>
        /// <returns>Path</returns>
        public static string ArtifactPath(LauncherConfig.ModPack modpack, string data, bool original)
        {
            string path;
            if (data.StartsWith("[") && data.EndsWith("]")) {
                data = data.Substring(1, data.Length - 2);
                var ext = ".jar";
                var split = data.Split(':');
                var index = split[^1].IndexOf("@", StringComparison.Ordinal);
                if (index != -1) {
                    ext = $".{split[^1].Substring(index + 1)}";
                    split[^1] = split[^1].Substring(0, index);
                }
                var package = split[0];
                var name = split[1];
                var version = split[2];
                var classifier = split.Length > 3 ? $"-{split[3]}" : "";
                //path = Path.Combine(FilesManager.Directories.LibrariesRoot,
                path = Path.Combine(FilesManager.Directories.GetLibrariesRoot(modpack),
                    package.Replace('.', Path.DirectorySeparatorChar),
                    name, version, $"{name}-{version}{classifier}{ext}");
            } else if (original) return data;
            else if (data.StartsWith('\'') && data.EndsWith("\'")) path = data;
            else path = Path.Combine(FilesManager.Directories.GetLibrariesRoot(modpack), data);

            if (Path.IsPathFullyQualified(path) && !Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        /// <summary>
        /// Get the addon JSON
        /// </summary>
        /// <param name="link">Link to the installer</param>
        /// <param name="main">Main JSON</param>
        /// <returns>The addon JSON</returns>
        //public static BlowaunchAddonJson GetAddonJson(string version, BlowaunchMainJson main, bool online)
        public static BlowaunchAddonJson GetAddonJson(LauncherConfig.ModPack selectedModPack, BlowaunchMainJson main, bool online)
        {
            string version = selectedModPack.Version.Id;
            var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
            string forgeDir = Path.Combine(selectedModPack.PackPath, "forge");
            if (!Directory.Exists(forgeDir))
            {
                Directory.CreateDirectory(forgeDir);
            }
            /*
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            return progress.Start(ctx => {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                string forgeFile = ForgeThingy.GetForgeFileByLink(main.Version);
                //var jar = Path.Combine(Path.GetTempPath(), ".blowaunch-forge", "installer.jar");
                var jar = Path.Combine(Directories.Root, "forge", forgeFile + ".installer.jar");
                var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
                var forgeDir = Path.Combine(Directories.Root, "forge");
                string forgeJsonFile = Path.Combine(forgeDir, $"version-{main.Version}.json");
                string content;
                
                if (!File.Exists(jar) || !File.Exists(forgeJsonFile))
                {
                    if (!online) return null;
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir); Fetcher.Download(ForgeThingy.GetLink(version), jar);
                    task.Description = "Extracting";
                    ZipFile.ExtractToDirectory(jar, dir);
                    task.Description = "Parsing";
                    content = File.ReadAllText(Path.Combine(dir, "version.json"));
                    //File.WriteAllText(forgeJsonFile, content);
                    var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(content);
                    o.Property("_comment_").Remove();
                    var sPrettyStr = JToken.Parse(o.ToString(Newtonsoft.Json.Formatting.None));
                    File.WriteAllText(forgeJsonFile, JsonConvert.SerializeObject(sPrettyStr, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText(Path.Combine(dir, "version.json"));
                }
                */
            string content = GetForgeData(CurentModPack, main, online);
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            return progress.Start(ctx => {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                var data = MojangLegacyMainJson.IsLegacyJson(content) 
                    ? BlowaunchMainJson.MojangToBlowaunchPartial(JsonConvert.DeserializeObject<MojangLegacyMainJson>(content)) 
                    : BlowaunchMainJson.MojangToBlowaunchPartial(JsonConvert.DeserializeObject<MojangMainJson>(content));
                task.Description = "Processing addon";
                var addon = new BlowaunchAddonJson {
                    Legacy = MojangLegacyMainJson.IsLegacyJson(content),
                    Author = "LexManos and contributors",
                    Information = "Black magic performed on the installer JAR",
                    BaseVersion = data.Version.Split('-')[0],
                    FullVersion = data.Version,
                    Arguments = new BlowaunchMainJson.JsonArguments(),
                    MainClass = data.MainClass
                };
                var libraries = new List<BlowaunchMainJson.JsonLibrary>();
                foreach (var lib in data.Libraries) {
                    if (string.IsNullOrEmpty(lib.Url)) {
                        var dest = Path.Combine(selectedModPack.PackPath, "forge", $"{lib.Name}-{lib.Version}.jar");
                        if (!Directory.Exists(Path.GetDirectoryName(dest))) Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        if (!File.Exists(dest)) File.Copy(Path.Combine(dir, "maven", lib.Path.Replace('/', 
                            Path.DirectorySeparatorChar)), dest);
                        lib.Url = $"file://{dest}"; // :bigbrain:
                        //lib.Url = $"https://maven.minecraftforge.net/{dest}";
                    }
                    libraries.Add(lib);
                }

                addon.Arguments.Game = data.Arguments.Game;
                addon.Arguments.Java = data.Arguments.Java;
                addon.Libraries = libraries.ToArray();
                task.StopTask();
                return addon;
            });
        }

        //public static string GetForgeData(string version, BlowaunchMainJson main, bool online)
        public static string GetForgeData(LauncherConfig.ModPack selectedModPack, BlowaunchMainJson main, bool online)
        {
            string version = selectedModPack.Version.Id.ToString();
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            return progress.Start(ctx =>
            {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                string forgeFile = ForgeThingy.GetForgeFileByLink(main.Version);
                //var jar = Path.Combine(Path.GetTempPath(), ".blowaunch-forge", "installer.jar");
                string forgeFileName = forgeFile == "" ? $"forge-{version}" : forgeFile;
                forgeFile = forgeFile == "" ? $"forge-{version}.jar" : forgeFile;
                //var jar = Path.Combine(Directories.Root, "forge", forgeFile + ".installer.jar");
                var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
                //var jar = Path.Combine(Directories.Root, "forge", forgeFileName + ".installer.jar");
                var jar = Path.Combine(dir, forgeFileName + ".installer.jar");
                var forgeDir = Path.Combine(selectedModPack.PackPath, "forge");
                string forgeJsonFile = Path.Combine(forgeDir, $"version-{main.Version}.json");
                string forgeInstallJsonFile = Path.Combine(forgeDir, $"install-{main.Version}.json");
                string content;
                string contentInstaller;

                //if (!File.Exists(jar) || !File.Exists(forgeJsonFile) || !File.Exists(forgeInstallJsonFile))
                if (!File.Exists(forgeJsonFile) || !File.Exists(forgeInstallJsonFile))
                {
                    if (!online) return "";
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir);
                    //string link = ForgeThingy.GetLink(version).GetAwaiter().GetResult();
                    if (CurentModPack.ModProxyVersion == null)
                    {
                        CurentModPack.ModProxyVersion = new Versions();
                        CurentModPack.ModProxyVersion.Url = ForgeThingy.GetLink(CurentModPack.Version.Id).GetAwaiter().GetResult();
                        //return "";
                    }
                    string link = CurentModPack.ModProxyVersion.Url;
                    Fetcher.Download(link, jar);
                 
                    task.Description = "Extracting";
                    ZipFile.ExtractToDirectory(jar, dir);
                    //ZipFile.ExtractToDirectory(jar, forgeDir);
                    task.Description = "Parsing";
                    content = File.ReadAllText(Path.Combine(dir, "version.json"));
                    contentInstaller = File.ReadAllText(Path.Combine(dir, "install_profile.json"));
                    //File.WriteAllText(forgeJsonFile, content);
                    var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(content);
                    var i = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(contentInstaller);
                    o.Property("_comment_").Remove();
                    i.Property("_comment_").Remove();
                    var sPrettyStr = JToken.Parse(o.ToString(Newtonsoft.Json.Formatting.None));
                    var sPrettyStrI = JToken.Parse(i.ToString(Newtonsoft.Json.Formatting.None));
                    File.WriteAllText(forgeJsonFile, JsonConvert.SerializeObject(sPrettyStr, Formatting.Indented));
                    File.WriteAllText(forgeInstallJsonFile, JsonConvert.SerializeObject(sPrettyStrI, Formatting.Indented));
                }
                else
                {
                    //content = File.ReadAllText(Path.Combine(dir, "version.json"));
                    content = File.ReadAllText(Path.Combine(forgeDir, $"version-{main.Version}.json"));
                    contentInstaller = File.ReadAllText(forgeInstallJsonFile);
                    
                }
                task.StopTask();
                return content;
            });
            
        }

        public static bool IsProcessorsExists(LauncherConfig.ModPack modpack, string version)
        {
            var forgeDir = Path.Combine(modpack.PackPath, "forge");
            var contentInstaller = File.ReadAllText(Path.Combine(forgeDir, $"install-{version}.json"));
            var dataInstaller = JsonConvert.DeserializeObject<ForgeInstallerJson>(contentInstaller);
            return dataInstaller.Processors != null;
        }

        /// <summary>
        /// Run processors
        /// </summary>
        /// <param name="main">Main JSON</param>
        /// <param name="online">Is in online mode</param>
        /// <returns>The addon JSON</returns>
        public static void RunProcessors(LauncherConfig.ModPack modpack, BlowaunchMainJson main, bool online)
        {
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            progress.Start(ctx => {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                var jar = Path.Combine(Path.GetTempPath(), ".blowaunch-forge", "installer.jar");
                var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
                var contentInstaller = File.ReadAllText(Path.Combine(modpack.PackPath, "forge",  $"install-{main.Version}.json"));
                var dataInstaller = JsonConvert.DeserializeObject<ForgeInstallerJson>(contentInstaller);
                if (File.Exists(Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"forge.json"))) {
                    AnsiConsole.WriteLine("[Forge] Skipping processors, already done!");
                    task.StopTask();
                }
                task.Description = "Processing artifact paths";
                var descriptors = new Dictionary<string, string>();
                foreach (var i in dataInstaller?.Data!)
                    descriptors.Add(i.Key, ArtifactPath(modpack, i.Value.Client, false));
                task.IsIndeterminate = false;
                task.MaxValue = dataInstaller!.Libraries.Length;
                foreach (var lib in dataInstaller.Libraries) {
                    var split = lib.Name.Split(":");
                    split[2] = split[2].Split('@')[0];
                    task.Description = $"Downloading {split[1]} v{split[2]}";
                    if (string.IsNullOrEmpty(lib.Downloads.Artifact.Url)) {
                        var dest = Path.Combine(FilesManager.Directories.Root, "forge", 
                            $"{split[1]}-{split[2]}{Path.GetExtension(lib.Downloads.Artifact.Path)}");
                        if (!Directory.Exists(Path.GetDirectoryName(dest))) Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        if (!File.Exists(dest)) File.Copy(Path.Combine(dir, "maven", lib.Downloads.Artifact.Path!.Replace('/', 
                            Path.DirectorySeparatorChar)), dest);
                        lib.Downloads.Artifact.Url = $"file://{dest}"; // :bigbrain:
                        continue;
                    }
                    
                    FilesManager.DownloadLibrary(new BlowaunchMainJson.JsonLibrary {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        Exclude = Array.Empty<string>(),
                        Extract = false,
                        Name = split[1],
                        Package = split[0],
                        Version = split[2],
                        Platform = "any",
                        ShaHash = lib.Downloads.Artifact.ShaHash,
                        Path = lib.Downloads.Artifact.Path,
                        Size = lib.Downloads.Artifact.Size,
                        Url = lib.Downloads.Artifact.Url
                        //}, main.Version, online);
                    }, modpack, online);
                    task.Increment(1);
                }

                task.Value = 0;
                task.Description = $"Running processors";
                task.MaxValue = dataInstaller.Processors.Length;
                // On early versions processors is absent
                if (dataInstaller.Processors.Length < 1)
                {
                    //RunWithoutProcessors(main, dataInstaller, online);
                }

                for (var index = 0; index < dataInstaller.Processors.Length; index++) {
                    var proc = dataInstaller.Processors[index];
                    if (proc.Sides != null && !proc.Sides.Contains("client")) {
                        AnsiConsole.WriteLine($"[Forge] Skipping processor {index + 1} - not for client!");
                        task.Increment(1);
                        continue;
                    }
                    var file = FilesManager.GetLibraryPath(modpack, new BlowaunchMainJson.JsonLibrary { Path = dataInstaller.Libraries
                        .FirstOrDefault(x => x.Name == proc.Jar)!.Downloads.Artifact.Path });
                    var mainClass = "<couldn't find>";
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs)) {
                        var ze = zf.GetEntry($"META-INF/MANIFEST.MF");
                        using (var s = zf.GetInputStream(ze))
                        using (var r = new StreamReader(s)) {
                            while (!r.EndOfStream) {
                                var line = r.ReadLine();
                                if (line.StartsWith("Main-Class: "))
                                    mainClass = line.Substring("Main-Class: ".Length,
                                        line.Length - "Main-Class: ".Length);
                            }
                        }
                    }

                    var classpath = new StringBuilder();
                    var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
                    foreach (var str in proc.Classpath) {
                        var file2 = FilesManager.GetLibraryPath(modpack, new BlowaunchMainJson.JsonLibrary { Path = dataInstaller.Libraries
                            .FirstOrDefault(x => x.Name == str)!.Downloads.Artifact.Path });
                        classpath.Append($"{file2}{separator}");
                    }

                    classpath.Remove(classpath.Length - 1, 1);
                    var args = new StringBuilder();
                    foreach (var arg in proc.Arguments) {
                        var replaced = arg.Replace("{ROOT}", FilesManager.Directories.Root)
                            .Replace("{INSTALLER}", jar)
                            .Replace("{MINECRAFT_JAR}", Path.Combine(FilesManager.Directories.VersionsRoot,
                                main.Version,
                                $"{main.Version}.jar")).Replace("{SIDE}", "client")
                            .Replace("{MINECRAFT_VERSION}", main.Version.Split('-')[0]);
                        foreach (var desc in descriptors)
                            replaced = replaced.Replace("{" + desc.Key + "}", desc.Value);
                        replaced = ArtifactPath(modpack, replaced, true);
                        if (replaced.StartsWith("/") || replaced.StartsWith("\\")) replaced = 
                            Path.Combine(Path.GetTempPath(), ".blowaunch-forge", replaced
                                .Substring(1, replaced.Length - 1)
                                .Replace('/', '\\'));
                        args.Append($"{replaced} ");
                    }

                    args.Remove(args.Length - 1, 1);
                    var command = $"-cp {file}{separator}{classpath} {mainClass} {args}";
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo {
                        WorkingDirectory = FilesManager.Directories.Root,
                        FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot, 
                            main.JavaMajor.ToString()), "bin", "java"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Arguments = command
                    };
                    process.OutputDataReceived += (_, e) => {
                        if (e.Data == null) return;
                        AnsiConsole.WriteLine(e.Data);
                    };
                    process.ErrorDataReceived += (_, e) => {
                        if (e.Data == null) return;
                        AnsiConsole.WriteLine(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    if (proc.Output != null) {
                        AnsiConsole.WriteLine($"[Forge] Verifying hashes...");
                        foreach (var n in proc.Output) {
                            var path = n.Key;
                            var sha1 = n.Value;
                            foreach (var desc in descriptors)
                                path = path.Replace("{" + desc.Key + "}", desc.Value);
                            foreach (var desc in descriptors)
                                sha1 = sha1.Replace("{" + desc.Key + "}", desc.Value);
                            var hash = HashHelper.Hash(path);
                            sha1 = sha1.Substring(1, sha1.Length - 2);
                            if (hash != sha1) {
                                AnsiConsole.MarkupLine($"[red]Hash mismatch for {path}![/]");
                                AnsiConsole.MarkupLine($"[red]Expected: {sha1}[/]");
                                AnsiConsole.MarkupLine($"[red]Actual: {hash}[/]");
                                Environment.Exit(0);
                            }
                        }
                    }
                    task.Increment(1);
                }
                task.StopTask();
            });
        }
        //public static void Run(BlowaunchMainJson main, BlowaunchAddonJson addonMain, Account account, string maxRam, bool customWindowSize, float width, float height, bool online, string gamePath)
        public static void _Run(BlowaunchMainJson main, BlowaunchAddonJson addonMain, Account account, bool online, LauncherConfig.ModPack modpack)
        {
            
            var classpath = new StringBuilder();
            var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";

            var OsDict = new Dictionary<string, string>() { 
            
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

            foreach (var library in main.Libraries)
            {
                bool currentOs = false;
                foreach (var allowedOs in library.Allow)
                {
                    currentOs = IsValidOs(allowedOs);
                }

                if (currentOs || library.Allow.Length == 0) 
                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && library.Allow.Contains("os-name:windows") || library.Allow.Length == 0)
                {
                    var file2 = FilesManager.GetLibraryPath(modpack, new BlowaunchMainJson.JsonLibrary
                    {
                        Path = library.Path
                    });
                    if (!File.Exists(file2))
                    {
                        FilesManager.DownloadLibrary(library, modpack, online);
                    }
                    classpath.Append($"{file2}{separator}");
                }
            }
            //classpath.Remove(classpath.Length - 1, 1);

            foreach (var library in addonMain.Libraries)
            {
                var file2 = FilesManager.GetLibraryPath(modpack, new BlowaunchMainJson.JsonLibrary
                {
                    Path = library.Path
                });
                if (!File.Exists(file2))
                {
                    FilesManager.DownloadLibrary(library, modpack, online);
                }
                classpath.Append($"{file2}{separator}");
            }
            if (ForgeThingy.IsProcessorsExists(modpack, main.Version))
            {
                ForgeThingy.RunProcessors(modpack, main, online);
            }
            //TODO Add check for dir and file exist
            string file = Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"{main.Version}.jar");
            string addonFile = Path.Combine(FilesManager.Directories.Root, "forge", $"{addonMain.FullVersion}.jar");
            classpath.Append($"{addonFile}{separator}");          


            var args = new StringBuilder();
            
            foreach (var arg in addonMain.Arguments.Game)
            {
                var replaced = arg.Value.Replace("${user_type}", "legacy")
                    //.Replace("${auth_access_token}", account.AccessToken)
                    //.Replace("${auth_uuid}", account.Uuid)
                    .Replace("${auth_access_token}", "0")
                    .Replace("${auth_uuid}", "0")
                    .Replace("${assets_index_name}", main.Assets.Id)
                    .Replace("${assets_root}", FilesManager.Directories.AssetsRoot)
                    .Replace("${game_directory}", modpack.PackPath) //FilesManager.Directories.Root)
                    .Replace("${version_name}", main.Version)
                    .Replace("${auth_player_name}", account.Name)
                    ;
                args.Append($"{replaced} ");
            }
            if (modpack.CustomWindowSize)
            {
                args.Append($"-width {modpack.WindowSize.X} ");
                args.Append($"-height {modpack.WindowSize.Y} ");
            }
            else
            {
                args.Append($"-width {840} ");
                args.Append($"-height {480} ");
            }

            var args2 = new StringBuilder();
            // its works only in java 8
            List<string> JavaArguments = new List<string> { "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions", "-XX:+AlwaysPreTouch", "-XX:+ParallelRefProcEnabled", "-Xms${xms}M", "-Xmx${xmx}M", "-Dfile.encoding=UTF-8", "-Dfml.ignoreInvalidMinecraftCertificates=true", "-Dfml.ignorePatchDiscrepancies=true", "-Djava.net.useSystemProxies=true", "-Djava.library.path=${native_path}"};
            //TODO: java args pass
            foreach (var arg in JavaArguments) {
                var javaArgsReplaced = arg.Replace("${xms}", "512")
                        .Replace("${xmx}", modpack.RamMax)
                        .Replace("${native_path}", Path.Combine( Directories.VersionsRoot, main.Version, "natives"));//"C:\\Users\\UserA\\AppData\\Roaming\\.blowaunch\\versions\\1.12.2\\natives");
                args2.Append($"{javaArgsReplaced} ");
            }


            args.Remove(args.Length - 1, 1);
            
            var command = $"{args2} -cp {file}{separator}{classpath} {addonMain.MainClass} {args}";
            
            if (main == null)
            {
                throw new ArgumentNullException(nameof(main));
            }
            var process = new Process();
            // JAVA_HOME and PATH sets here 
            string envPath = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("Path", Path.Combine(Directories.JavaRoot, "8", "bin") + ";" + envPath, EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("JAVA_HOME", Path.Combine(Directories.JavaRoot, "8"), EnvironmentVariableTarget.User);
            process.StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = FilesManager.Directories.Root,
                FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot,
                    main.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw"),
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                UseShellExecute = true,
                Arguments = command
            };

            Console.WriteLine(Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot,
                    main.JavaMajor.ToString()), "bin", !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java" : "javaw") + " " + command);
            
            process.OutputDataReceived += (_, e) => {
                if (e.Data == null) return;
                AnsiConsole.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) => {
                if (e.Data == null) return;
                AnsiConsole.WriteLine(e.Data);
            };
            process.Start();
            process.WaitForExit();
            // Secure errror on start
            // https://github.com/OpenFeign/feign/issues/935#issuecomment-521236281
        }
        /*
        public static void RunWithoutProcessors(BlowaunchMainJson main, ForgeInstallerJson dataInstaller, bool online)
        {
            var file = FilesManager.GetLibraryPath(new BlowaunchMainJson.JsonLibrary
            {
                Path = dataInstaller.Libraries
                       .FirstOrDefault(x => x.Name == proc.Jar)!.Downloads.Artifact.Path
            });
            var mainClass = "<couldn't find>";
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs))
            {
                var ze = zf.GetEntry($"META-INF/MANIFEST.MF");
                using (var s = zf.GetInputStream(ze))
                using (var r = new StreamReader(s))
                {
                    while (!r.EndOfStream)
                    {
                        var line = r.ReadLine();
                        if (line.StartsWith("Main-Class: "))
                            mainClass = line.Substring("Main-Class: ".Length,
                                line.Length - "Main-Class: ".Length);
                    }
                }
            }

            var classpath = new StringBuilder();
            var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
            foreach (var str in proc.Classpath)
            {
                var file2 = FilesManager.GetLibraryPath(new BlowaunchMainJson.JsonLibrary
                {
                    Path = dataInstaller.Libraries
                    .FirstOrDefault(x => x.Name == str)!.Downloads.Artifact.Path
                });
                classpath.Append($"{file2}{separator}");
            }

            classpath.Remove(classpath.Length - 1, 1);
            var args = new StringBuilder();
            foreach (var arg in proc.Arguments)
            {
                var replaced = arg.Replace("{ROOT}", FilesManager.Directories.Root)
                    .Replace("{INSTALLER}", jar)
                    .Replace("{MINECRAFT_JAR}", Path.Combine(FilesManager.Directories.VersionsRoot,
                        main.Version,
                        $"{main.Version}.jar")).Replace("{SIDE}", "client")
                    .Replace("{MINECRAFT_VERSION}", main.Version.Split('-')[0]);
                foreach (var desc in descriptors)
                    replaced = replaced.Replace("{" + desc.Key + "}", desc.Value);
                replaced = ArtifactPath(replaced, true);
                if (replaced.StartsWith("/") || replaced.StartsWith("\\")) replaced =
                    Path.Combine(Path.GetTempPath(), ".blowaunch-forge", replaced
                        .Substring(1, replaced.Length - 1)
                        .Replace('/', '\\'));
                args.Append($"{replaced} ");
            }

            args.Remove(args.Length - 1, 1);
            var command = $"-cp {file}{separator}{classpath} {mainClass} {args}";
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = FilesManager.Directories.Root,
                FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot,
                    main.JavaMajor.ToString()), "bin", "java"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = command
            };
            process.OutputDataReceived += (_, e) => {
                if (e.Data == null) return;
                AnsiConsole.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) => {
                if (e.Data == null) return;
                AnsiConsole.WriteLine(e.Data);
            };
            process.Start();
        }
        */
        static public string GetForgeFileByLink(string version)
        {
            Regex regex = new Regex($"forge-{version}-(.*).jar$");
            var matches = Directory.EnumerateFiles(Directories.Forge).Where(f => regex.IsMatch(f));
            List<int[]> IntVersion = new List<int[]>();
            foreach (var match in matches)
            {
                Console.WriteLine(match);
                string fileName = match.Split(Path.DirectorySeparatorChar).Last().Replace(".jar", "").Replace($"forge-{version}-", "");
                var versions = fileName.Split(".");
                if (versions.Length > 0)
                {
                    //IntVersion.Add(new int[] { Int32.Parse(versions[0]), Int32.Parse(versions[1]), Int32.Parse(versions[2]), Int32.Parse(versions[3]) });
                    List<int> tmpVersions = new List<int>();
                    for (int i = 0; i < versions.Length;i++)
                    {
                        tmpVersions.Add(Int32.Parse(versions[i]));
                    }
                    IntVersion.Add(tmpVersions.ToArray());
                }
                //fileName = ;
            }
            int[] lastVersion = new int[4];
            foreach(var verArray in IntVersion)
            {
                //for (int i = 0; i <= 3; i++) {
                for (int i = 0; i < verArray.Length; i++)
                {
                    lastVersion[i] = verArray[i] > lastVersion[i] ? verArray[i] : lastVersion[i];
                }
            }
            //IntVersion.Select(x => x.Select())
            if(lastVersion[0] != 0)
            {
                //return $"forge-{version}-{String.Join(".", lastVersion)}.jar";
                return $"forge-{version}-{String.Join(".", lastVersion)}";
            }
            return "";
        }

        public static bool ForgeIsInstalled(string version)
        {
            // TODO: read .blowaunch\forge\install-1.19.2.json and .blowaunch\forge\version-1.19.2.json and check for exist all libraries
            return File.Exists(Path.Combine(Directories.Forge, "install-1.19.2.json")) &&
                File.Exists(Path.Combine(Directories.Forge, "version-1.19.2.json"));
        }
    }
}