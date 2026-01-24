using Serilog;
using Serilog.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Novacraft.Library;

public static class Fetcher
{
    public static class MojangEndpoints
    {
        public const string Versions = "http://launchermeta.mojang.com/mc/game/version_manifest.json";
        public const string Library =
            "https://libraries.minecraft.net/{0}/{1}/{2}/{1}-{2}{3}.jar";
        public const string Asset = "https://resources.download.minecraft.net/{0}/{1}";
    }

    public static class FabricEndpoints
    {
        public const string VersionLoaders = "https://meta.fabricmc.net/v2/versions/loader/{0}";
        public const string LoaderJson = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}/profile/json";
    }

    public static class NovacraftEndpoints
    {
        //public const string OpenJdk = "https://github.com/TheAirBlow/Novacraft/raw/main/openjdk.json";
        //public const string OpenJdk = "https://raw.githubusercontent.com/pdragon/NovacraftData/main/openjdk.json";
        public const string OpenJdk = "https://raw.githubusercontent.com/pdragon/NovaCraft/refs/heads/main/openjdk.json";
        
        
    }

    public static class ForgeEndpoints
    {
        public const string ForgeWebsite = "https://files.minecraftforge.net/net/minecraftforge/forge/index_{0}.html";
    }

    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("Novacraft.log")
        .WriteTo.Console()
        .CreateLogger();

    public static string Fetch(string url)
    {
        //using var wc = new WebClient();
        //return wc.DownloadString(url);
        using var wc = new HttpClient();
        var content = wc.GetAsync(url).GetAwaiter().GetResult().Content;
        return content.ReadAsStringAsync().Result;
    }

    public static string Fetch(string url, short times)
    {
        //using var wc = new WebClient();
        //return wc.DownloadString(url);
        using var wc = new HttpClient();
        //bool error = false;
        short tryingTimes = times;
        while (tryingTimes > 0)
        { 
            try
            {
                var content = wc.GetAsync(url).GetAwaiter().GetResult().Content;
                return content.ReadAsStringAsync().Result;
            }
            catch (Exception ex) {
                Logger.Warning($"Probably Network timeout ({ex.Message}) while trying loading {url}, {tryingTimes} attempts left");
                tryingTimes--;
                //error = true;
            }
        }
        return null;
    }

    public static void Download(string url, string path)
    {
        using var client = new WebClient();
        client.DownloadFile(url, path);
    }
}