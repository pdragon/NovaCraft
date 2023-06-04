using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Blowaunch.Library;

/// <summary>
/// Blowaunch Files Manager
/// </summary>
public static class FilesManager
{
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
        var indexPath = Path.Combine(Directories.AssetsRoot, "indexes", String.Join(".", version.Split(".").SkipLast(1)) + ".json");
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
    public static string GetLibraryPath(BlowaunchMainJson.JsonLibrary library)
        => Path.Combine(Directories.LibrariesRoot, library.Path.Replace('/', Path.DirectorySeparatorChar));

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
    public static void DownloadLibrary(BlowaunchMainJson.JsonLibrary library, string version, bool online)
    {
        var path = GetLibraryPath(library);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var debug = $"{library.Package}:{library.Name}:{library.Version}:{library.Platform}";
        if (!File.Exists(path) && online) Fetcher.Download(library.Url, path);
            
        var hash = HashHelper.Hash(path);
        if (hash != library.ShaHash) {
            if (online) {
                AnsiConsole.MarkupLine($"[yellow]{debug} hash mismatch: {hash} and {library.ShaHash}, redownloading...[/]");
                File.Delete(path);
                DownloadLibrary(library, version, true);
            } else AnsiConsole.MarkupLine($"[yellow]{debug} hash mismatch: {hash} and {library.ShaHash}, " +
                                          $"can't redownload in offline mode![/]");
        }
            
        if (library.Extract) {
            var natives = Path.Combine(Directories.VersionsRoot, version, "natives");
            if (!Directory.Exists(natives)) Directory.CreateDirectory(natives);
            ZipFile.ExtractToDirectory(path, natives, true);
            foreach (var i in library.Exclude) {
                if (File.Exists(i)) File.Delete(i);
                if (Directory.Exists(i)) Directory.Delete(i, true);
            }
        }
    }

    /// <summary>
    /// Downloads client and all required files
    /// </summary>
    /// <param name="main">Blowaunch Main JSON</param>
    /// <param name="online">Is in online mode</param>
    public static void DownloadClient(BlowaunchMainJson main, bool online)
    {
        var path = Path.Combine(Directories.VersionsRoot, main.Version);
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

    public static List<BlowaunchMainJson.JsonLibrary> GetForgeLibrariesPaths(string version)
    {
        var result = new List<BlowaunchMainJson.JsonLibrary>();
        
        var ext = new List<string> { "json" };
        var dir = Path.Combine(Directories.VersionsRoot, version);
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
}