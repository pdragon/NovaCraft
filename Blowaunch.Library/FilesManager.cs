using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
namespace Blowaunch.Library;

/// <summary>
/// Blowaunch Files Manager
/// </summary>
public static class FilesManager
{
    public enum JavaDownloadError : short{
        UnableToFindOpenJDK,
        OSIsNotSupported,
        AlreadyDownloaded,
        OfflineMode,
        Success
    };

    /// <summary>
    /// Minecraft Directories
    /// </summary>
    public static class Directories
    {
        public static readonly string Root =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".blowaunch");
        //public static readonly string Root =
        //    Path.Combine(Path.Combine("r:\\public\\share\\"), ".blowaunch");
        public static readonly string AssetsRoot =
            Path.Combine(Root, "assets");
        public static readonly string AssetsObjects =
            Path.Combine(AssetsRoot, "objects");
        public static readonly string AssetsObject =
            Path.Combine(AssetsRoot, "objects");
        public static readonly string AssetsIndexes =
            Path.Combine(AssetsRoot, "indexes");
        public static readonly string LibrariesRoot =
            Path.Combine(Root, "libraries");
        public static readonly string VersionsRoot =
            Path.Combine(Root, "versions");
        public static readonly string JavaRoot =
            Path.Combine(Root, "runtime");
        public static readonly string Forge =
            Path.Combine(Root, "forge");

       public static string GetLibrariesRoot(LauncherConfig.ModPack modpack)
        {
            return modpack != null ? Path.Combine(Root, modpack.PackPath, "libraries") : Path.Combine(Root, "libraries");
        }
    }

    private static LauncherConfig.ModPack curentModPack;

    public static LauncherConfig.ModPack CurentModPack
    {
        get => curentModPack??GetLastStartedModPack();  set => curentModPack = value; 
    }

    public static LauncherConfig.ModPack GetLastStartedModPack()
    {
        LauncherConfig Config = new();
        try
        {
            Config = JsonConvert.DeserializeObject
                <LauncherConfig>(File.ReadAllText(
                    "config.json"))!;
        }
        catch (Exception)
        {
            return new LauncherConfig.ModPack();
        }
        return Config.ModPacks.OrderByDescending(t => t.Time).FirstOrDefault();
    }


    /// <summary>
    /// Initialize directories
    /// </summary>
    public static void InitializeDirectories()
    {
        Directory.CreateDirectory(Directories.Root);
        Directory.CreateDirectory(Directories.AssetsRoot);
        Directory.CreateDirectory(Directories.AssetsObjects);
        Directory.CreateDirectory(Directories.AssetsIndexes);
        Directory.CreateDirectory(Directories.LibrariesRoot);
        Directory.CreateDirectory(Directories.VersionsRoot);
    }


    public static MojangAssetsJson LoadMojangAssets(string version, bool online, BlowaunchMainJson mainJSON = null)
    {
        MojangAssetsJson actualJson = new MojangAssetsJson();
        if (mainJSON == null)
        {
            mainJSON = (MojangFetcher.GetMain(version));
        }
        var path = Path.Combine(Directories.AssetsObject, mainJSON.Assets.ShaHash.Substring(0, 2), mainJSON.Assets.ShaHash);
        var indexPath = Path.Combine(Directories.AssetsRoot, "indexes", mainJSON.Assets.Id + ".json");// String.Join(".", version.Split(".").SkipLast(1)) + ".json");
        //var indexPath = Path.Combine(Directories.AssetsRoot, "indexes", String.Join(".", version) + ".json");
        if (!File.Exists(path)){
            if (online)
                DownloadMojangAssetsJson(version, true);
            else
                return null;
        }
        if (!File.Exists(indexPath))
        {
            if (!Directory.Exists(Path.Combine(Directories.AssetsRoot, "indexes")))
            {
                Directory.CreateDirectory(Path.Combine(Directories.AssetsRoot, "indexes"));
            }
            File.Copy(path, indexPath);
        }
        var json = File.ReadAllText(path);
        dynamic d = JObject.Parse(json);

        if (MojangAssetsJson.IsMojangAssetsJson(d))
        {
            return JsonConvert.DeserializeObject<MojangAssetsJson>(json);
        }
        return null;
    }

    public static void DownloadMojangAssetsJson(string version, bool online)
    {
        BlowaunchMainJson main = (MojangFetcher.GetMain(version));
        BlowaunchAssetsJson.JsonAsset jsonAsset = new BlowaunchAssetsJson.JsonAsset()
        {
            Name = main.Assets.Id,
            ShaHash = main.Assets.ShaHash,
            Size = main.Assets.Size ?? 0,
            Url = main.Assets.Url
        };
        FilesManager.DownloadAsset(jsonAsset, online);
    }

    /// <summary>
    /// Downloads an asset
    /// </summary>
    /// <param name="asset">Blowaunch Asset JSON</param>
    /// <param name="online">Is in online mode</param>
    public static void DownloadAsset(BlowaunchAssetsJson.JsonAsset asset, bool online)
    {
        var path = Path.Combine(Directories.AssetsObject, asset.ShaHash.Substring(0, 2), asset.ShaHash);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) && online)
        {
            AnsiConsole.MarkupLine($"[green] Downoading asset: {asset.Name} ({asset.ShaHash}), " + $"[/]");
            Fetcher.Download(asset.Url, path);
        }
            
        var hash = HashHelper.Hash(path);
        if (hash != asset.ShaHash) {
            if (online) {
                AnsiConsole.MarkupLine($"[yellow]{asset.Name} hash mismatch: {hash} and {asset.ShaHash}, redownloading...[/]");
                File.Delete(path);
                DownloadAsset(asset, true);
            } else AnsiConsole.MarkupLine($"[yellow]{asset.Name} hash mismatch: {hash} and {asset.ShaHash}, " +
                                          $"can't redownload in offline mode![/]");
        }
    }

    /// <summary>
    /// Get library path
    /// </summary>
    /// <param name="library">Blowaunch Library JSON</param>
    /// <returns>Path</returns>
    public static string GetLibraryPath(LauncherConfig.ModPack modpack, BlowaunchMainJson.JsonLibrary library)
    //    => Path.Combine(Directories.LibrariesRoot, library.Path.Replace('/', Path.DirectorySeparatorChar));
        => Path.Combine(Directories.GetLibrariesRoot(modpack), library.Path.Replace('/', Path.DirectorySeparatorChar));
    //public static string GetLibraryPath(BlowaunchMainJson.JsonLibrary library)
    //    => Path.Combine(CurentModPack.PackPath, "libraries", library.Path.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Get library path
    /// </summary>
    /// <param name="library">Mojang Library JSON</param>
    /// <returns>Path</returns>
    public static string GetMojangLibraryPath( MojangMainJson.JsonLibrary library)
        => Path.Combine(Directories.LibrariesRoot, library.Downloads.Artifact.Path.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Downloads a library
    /// </summary>
    /// <param name="library">Blowaunch Library JSON</param>
    /// <param name="version">Version</param>
    /// <param name="online">Is in online mode</param>
    [SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
    //public static void DownloadLibrary(BlowaunchMainJson.JsonLibrary library, string version, bool online)
    public static void DownloadLibrary(BlowaunchMainJson.JsonLibrary library, LauncherConfig.ModPack modpack, bool online)
    {
        string version = modpack.Version.Id;
        var path = GetLibraryPath(modpack, library);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var debug = $"{library.Package}:{library.Name}:{library.Version}:{library.Platform}";
        try
        {
            if (!File.Exists(path) && online) 
                Fetcher.Download(library.Url, path);
        }catch (Exception ex)
        {
            Console.WriteLine(ex.Message.ToString());
        }
            
        var hash = HashHelper.Hash(path);
        if (hash != library.ShaHash && (File.Exists(path) && library.ShaHash != null || !File.Exists(path)))
        {
            if (online)
            {
                if (File.Exists(path))
                {
                    AnsiConsole.MarkupLine($"[yellow]{debug} hash mismatch: {hash} and {library.ShaHash}, redownloading...[/]");
                    File.Delete(path);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green] Donwloading {debug}[/]");
                }
                //DownloadLibrary(library, version, true);
                DownloadLibrary(library, modpack, true);
            }
            else AnsiConsole.MarkupLine($"[yellow]{debug} hash mismatch: {hash} and {library.ShaHash}, " +
                                            $"can't redownload in offline mode![/]"); 
        }
            
        if (library.Extract) {
            //var natives = Path.Combine(Directories.VersionsRoot, version, "natives");
            var natives = Path.Combine(modpack.PackPath, version, "natives");
            if (!Directory.Exists(natives)) Directory.CreateDirectory(natives);
            ZipFile.ExtractToDirectory(path, natives, true);
            foreach (var i in library.Exclude) {
                if (File.Exists(i)) File.Delete(i);
                if (Directory.Exists(i)) Directory.Delete(i, true);
            }
        }
    }

    public static string GetForgeLastVersion(string link)
    {
        if (link == null) return null;
        return "";
    }
    
    /// <summary>
    /// Downloads client and all required files
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="online">Is in online mode</param>
    public static void DownloadClient(LauncherConfig.ModPack modpack, BlowaunchMainJson main, bool online)
    {
        //var path = Path.Combine(Directories.VersionsRoot, main.Version);
        var path = Path.Combine(modpack.PackPath, main.Version);
        var version = Path.Combine(path, $"{main.Version}.jar");
        var logging = Path.Combine(path, "logging.xml");
        Directory.CreateDirectory(path);
        //if (online) {
        //    Fetcher.Download(main.Downloads.Client.Url, version);
        //    Fetcher.Download(main.Logging.Download.Url, logging);
        //}
            
        //if (main.Version.Contains("forge"))
        //    AnsiConsole.MarkupLine("[yellow]Skipping hash checks, Forge will patch the client[/]");
        var hash1 = HashHelper.Hash(version);
        if (hash1 != main.Downloads.Client.ShaHash) {
            if (online) {
                AnsiConsole.MarkupLine($"[yellow]{version} hash mismatch: {hash1} and {main.Downloads.Client.ShaHash}, redownloading...[/]");
                //DownloadClient(main, true);
                Fetcher.Download(main.Downloads.Client.Url, version);
            } else AnsiConsole.MarkupLine($"[yellow]{version} hash mismatch: {hash1} and {main.Downloads.Client.ShaHash}, " +
                                          $"can't redownload in offline mode![/]");
        }
        var hash2 = HashHelper.Hash(logging);
        if (hash2 != main.Logging.Download.ShaHash) {
            if (online) {
                AnsiConsole.MarkupLine($"[yellow]{logging} hash mismatch: {hash2} and {main.Logging.Download.ShaHash}, redownloading...[/]");
                //DownloadClient(main, true);
                Fetcher.Download(main.Logging.Download.Url, logging);
            } else AnsiConsole.MarkupLine($"[yellow]{logging} hash mismatch: {hash2} and {main.Logging.Download.ShaHash}, " +
                                          $"can't redownload in offline mode![/]");
        }
    }
    /*
    public static void DownloadForge(string version, bool online)
    {
        var forgeInstall = GetForgeInstallData(version);
        var dirName = Path.Combine(Directories.VersionsRoot + Path.DirectorySeparatorChar.ToString(), version);
        if (forgeInstall != null)
        {
            AnsiConsole.MarkupLine($"[green] Donwnloading forge[/]");
            foreach (var library in forgeInstall.Libraries)
            {
                if (library.Downloads.Artifact != null && library.Downloads.Artifact.Url != null && library.Downloads.Artifact.Path != null) {
                    if (library.Downloads.Artifact.Url != "")
                    {
                        //TODO: check for exist
                        var path = GetMojangLibraryPath(library);
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        if (!File.Exists(path))
                        {
                            AnsiConsole.MarkupLine($"[green] Donwnloading forge library: {library.Downloads.Artifact.Path}[/]");
                            Fetcher.Download(library.Downloads.Artifact.Url, path);
                        }
                    }
                    else
                    {
                        var filename = library.Downloads.Artifact.Path.Split("/").ToList().Last();
                        //var extractPath = Path.Combine(Directories.LibrariesRoot, library.Downloads.Artifact.Path.Replace('/', Path.DirectorySeparatorChar), filename);
                        var extractPath = Path.Combine(dirName, filename);
                        if (!File.Exists(extractPath))
                        {
                            ExtractFile(Path.Combine(dirName, $"{version}-installer.jar"), "maven/"+library.Downloads.Artifact.Path, extractPath);
                        }
                    }
                }
               
            }
        }
        
    }

    /// <summary>
    /// Get libraries from json file 
    /// </summary>
    public static ForgeInstallerJson GetForgeInstallData(string versionStr)
    {
        List<string> installFiles = new List<string> { "install_profile.json", "version.json" };
        //string versionStr = "1.12.2-forge-14.23.5.2859";
        FetchLinkToForgeInstallFile("1.12.2");
        //string sourceInstallFileName = "version";
        ForgeInstallerJson actualJson = new ForgeInstallerJson();
        var dirName = Path.Combine(Directories.VersionsRoot + Path.DirectorySeparatorChar.ToString(), versionStr);
        //ExtractFile(Path.Combine(dirName, $"{versionStr}-installer.jar"), $"{versionStr}.jar", null);
        if (!Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }
        var libraries = new List<MojangMainJson.JsonLibrary>();
        var forgeInstallerJson = new ForgeInstallerJson();
        foreach (string filename in installFiles)
        {
            ExtractFile(Path.Combine(dirName, $"{versionStr}-installer.jar"), filename, Path.Combine(dirName, $"{filename}"));
            //var jsonPath = Path.Combine(dirName, $"{filename}.json");
            var jsonPath = Path.Combine(dirName, $"{filename}");
            var json = File.ReadAllText(jsonPath);
            dynamic d = JObject.Parse(json);
            if (ForgeInstallerJson.IsOwnJson(d)) {
                actualJson = JsonConvert.DeserializeObject<ForgeInstallerJson>(json);
                
                foreach (var library in actualJson.Libraries)
                {
                    libraries.Add(library);
                }
            }
        }
        forgeInstallerJson.Libraries = libraries.ToArray();
        return forgeInstallerJson;
    }

    //public static ZipArchiveEntry ExtractFile(string filename, string path)
    public static void ExtractFile(string filename, string pathInArchive, string extractPath)
    {
        if(extractPath == null)
        {
            throw new ArgumentNullException("extract path is empty");
        }
        if(filename == null)
        {
            throw new ArgumentNullException("filename is empty");
        }
        using (ZipArchive archive = new ZipArchive(File.Open(filename, FileMode.Open)))
        {
            //TODO: realize erase all files button (if some files will have size = 0 in some cases)
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if(entry.FullName == pathInArchive && !File.Exists(extractPath))
                {
                    entry.ExtractToFile(extractPath);
                }
            }
        }
    }

    public static void FetchLinkToForgeInstallFile(string version)
    {
        
    }
    */
    public static string GetFirstForgeVersion(string version)
    {
        var pattern = "forge-" + version + "-*";
        var directories = Directory.GetDirectories(Directories.VersionsRoot, pattern);
        var result = directories.ToList<string>();
        if(directories.Count() > 0)
        {

            return directories[0].Split(Path.DirectorySeparatorChar).Last();
        }
        return null;
    }

    public static List<BlowaunchMainJson.JsonLibrary> GetForgeLibrariesPaths(LauncherConfig.ModPack modpack, string version)
    {
        var result = new List<BlowaunchMainJson.JsonLibrary>();
        
        var ext = new List<string> { "json" };
        //var dir = Path.Combine(Directories.VersionsRoot, version);
        var dir = Path.Combine(modpack.PackPath, version);
        var installFiles = Directory
            .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
            .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

        var libraries = new List<BlowaunchMainJson.JsonLibrary>();
        var forgeInstallerJson = new ForgeInstallerJson();
        foreach (string filename in installFiles)
        {
            var jsonPath = Path.Combine(dir, $"{filename}");
            var json = File.ReadAllText(jsonPath);
            dynamic d = JObject.Parse(json);
            if (ForgeInstallerJson.IsOwnJson(d))
            {
                var actualJson = JsonConvert.DeserializeObject<ForgeInstallerJson>(json);

                foreach (var library in actualJson.Libraries)
                {

                    result.Add(new BlowaunchMainJson.JsonLibrary
                    {
                        Path = library.Downloads.Artifact.Path,
                        Name = library.Name,
                        Allow = new String[0],
                        Disallow = new String[0],
                    });
                }
            }
        }
        return result;
    }

    public static JavaDownloadError JavaDownload(BlowaunchMainJson main, ProgressTask task, bool online)
    {
        var dir = Path.Combine(FilesManager.Directories.JavaRoot, main.JavaMajor.ToString());
        var extract = Path.Combine(FilesManager.Directories.JavaRoot);
        if (online)
        {
            if (!Directory.Exists(dir))
            {
                if (task != null)
                {
                    task.Description = "Fetching";
                }
                var openjdk = JsonConvert.DeserializeObject<OpenJdkJson>(Fetcher.Fetch(Fetcher.BlowaunchEndpoints.OpenJdk));
                if (!openjdk!.Versions.ContainsKey(main.JavaMajor))
                {
                    AnsiConsole.MarkupLine($"[red]Unable to find OpenJDK version {main.JavaMajor}![/]");
                    AnsiConsole.MarkupLine($"[red]Please report it to us on the GitHub issues page.[/]");
                    return JavaDownloadError.UnableToFindOpenJDK;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    AnsiConsole.WriteLine("[OpenJDK] Detected Windows!");
                    var link = openjdk.Versions[main.JavaMajor].Windows;
                    var path = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(link)!);
                    if (task != null)
                    {
                        task.Description = "Downloading";
                    }
                    Fetcher.Download(link, Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(link)!));
                    if (task != null)
                    {
                        task.Description = "Extracting";
                    }
                    ZipFile.ExtractToDirectory(path,
                        extract, true);
                    if (task != null)
                    {
                        task.Description = "Renaming";
                    }
                    Directory.Move(Path.Combine(extract, openjdk.Versions[main
                        .JavaMajor].Directory), dir);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    AnsiConsole.WriteLine("[OpenJDK] Detected Linux!");
                    var link = openjdk.Versions[main.JavaMajor].Linux;
                    var path = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(link)!);
                    if (task != null)
                    {
                        task.Description = "Downloading";
                    }
                    Fetcher.Download(link, path);
                    if (task != null)
                    {
                        task.Description = "Extracting";
                    }
                    ExtractTar(path, extract);
                    if (task != null)
                    {
                        task.Description = "Renaming";
                    }
                    Directory.Move(Path.Combine(extract, openjdk.Versions[main
                        .JavaMajor].Directory), dir);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    AnsiConsole.WriteLine("[OpenJDK] Detected MacOS!");
                    var link = openjdk.Versions[main.JavaMajor].MacOs;
                    var path = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(link)!);
                    if (task != null)
                    {
                        task.Description = "Downloading";
                    }
                    Fetcher.Download(link, Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(link)!));
                    if (task != null)
                    {
                        task.Description = "Extracting";
                    }
                    ExtractTar(path, extract);
                    if (task != null)
                    {
                        task.Description = "Renaming";
                    }
                    Directory.Move(Path.Combine(extract, openjdk.Versions[main
                        .JavaMajor].Directory), dir);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Your OS is not supported![/]");
                    return JavaDownloadError.OSIsNotSupported;
                }
            }
            else
            {
                AnsiConsole.WriteLine("[OpenJDK] Skipping, already downloaded!");
                return JavaDownloadError.AlreadyDownloaded;
            }
        }
        else
        {
            AnsiConsole.WriteLine("[OpenJDK] Skipping, we are in offline mode");
            return JavaDownloadError.OfflineMode;
        }
        return JavaDownloadError.Success;
    }

    public static void ExtractTar(string path, string directory)
    {
        var dataBuffer = new byte[4096];
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var gzipStream = new GZipInputStream(fs);
        using var fsOut = File.OpenWrite(directory);
        fsOut.Seek(0, SeekOrigin.Begin);
        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
    }

}