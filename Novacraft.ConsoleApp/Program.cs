﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Novacraft.Library;
using Novacraft.Library.Authentication;
using Newtonsoft.Json;
using Spectre.Console;

namespace Novacraft.ConsoleApp
{
    public static class Program
    {
        //private static bool CheckForInternet(int timeoutMs = 5000)
        private static bool CheckForInternet(int timeoutSec = 5)
        {
            try {
                //var request = (HttpWebRequest)WebRequest.Create("https://google.com");
                //var request = WebRequest.Create("https://google.com");
                //request.KeepAlive = false;
                //request.Timeout = timeoutMs;
                //using var response = (HttpWebResponse)request.GetResponse();
                using var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://google.com");
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
                var response = httpClient.Send(request);
                return true;
            } catch { return false; }
        }
        
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AnsiConsole.MarkupLine($"[red]An unexpected error has occured! Below are the logs.[/]");
            AnsiConsole.MarkupLine($"[red]Please report it to us on the GitHub issues page.[/]");
        }
        
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            AnsiConsole.MarkupLine($"[yellow]Welcome to Novacraft v{Assembly.GetExecutingAssembly().GetName().Version!}![/]");
            AnsiConsole.MarkupLine($"[yellow]Written completely from scratch, but is based on deprecated Node.js version[/]");
            AnsiConsole.MarkupLine($"[yellow]Official GitHub: https://github.com/theairblow/Novacraft[/]");

            if (!File.Exists("config.json")) {
                AnsiConsole.MarkupLine("[red]config.json does not exist![/]");
                AnsiConsole.MarkupLine("[red]An empty one was created.[/]");
                File.WriteAllText("config.json", JsonConvert
                    .SerializeObject(new Runner.Configuration(), Formatting.Indented));
                return;
            }

            var json = JsonConvert.DeserializeObject<Runner.Configuration>(
                File.ReadAllText("config.json"));
            NovacraftMainJson mainJson = null;
            NovacraftAddonJson addonJson = null;
            
            var online = CheckForInternet();
            if (json.ForceOffline) online = false;
            AnsiConsole.MarkupLine(json.ForceOffline 
                ? $"[yellow]Offline mode forced by configuration[/]"
                : online 
                    ? $"[green]Internet connection present, online mode[/]"
                    : $"[yellow]No internet, offline mode[/]");

            //if (!online && json.Auth.Type != Runner.Configuration.AuthClass.AuthType.None)
            if (!online && json.Account.Type != Account.AuthType.None)
                AnsiConsole.MarkupLine("[yellow]No authentication will be performed in offline mode![/]");
            else {
                //switch (json.Auth.Type) {
                //Temporaly comment because I can't have microsoft account to check and edit this one
                /*
                switch (json.Account.Type)
                {
                    case Runner.Configuration.AuthClass.AuthType.Microsoft:
                        if (File.Exists("microsoft.json")) {
                            if (!MicrosoftAuth.Authenticate(File.ReadAllText(
                                    "microsoft.json"), ref json)) {
                                AnsiConsole.MarkupLine($"[red]Authentication failed! Please try again.[/]");
                                return;
                            }
                            File.Delete("microsoft.json");
                        } 
                        
                        if (!MicrosoftAuth.CheckAuth(ref json)) {
                            AnsiConsole.MarkupLine($"[red]Please authenticate and save the JSON as \"microsoft.json\"![/]");
                            MicrosoftAuth.OpenAuth();
                            return;
                        } else AnsiConsole.MarkupLine($"[green]Succeffully authenticated![/]");
                        File.WriteAllText("config.json", JsonConvert
                            .SerializeObject(new Runner.Configuration(), Formatting.Indented));
                        break;
                }
                */
            }

            var mainJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"version.json");
            var addonJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"addon.json");
            var addonFabricJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"fabric.json");
            var addonForgeJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"forge.json");
            var command = "";
            switch (json.Type) {
                case Runner.Configuration.VersionType.OfficialMojang:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        try { mainJson = MojangFetcher.GetMain(json.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version JSON![/]");
                            return;
                        }
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(File.ReadAllText(mainJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(null, mainJson, online);
                    //command = Runner.GenerateCommand(mainJson, json);
                    command = Runner.GenerateCommand(null, mainJson, json);
                    break;
                case Runner.Configuration.VersionType.CustomVersionFromDir:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version,
                            $"{json.Version}.json")));
                    MainDownloader.DownloadAll(null, mainJson, online);
                    //command = Runner.GenerateCommand(mainJson, json);
                    command = Runner.GenerateCommand(null, mainJson, json);
                    break;
                case Runner.Configuration.VersionType.CustomWithAddonConfig:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                    mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version,
                            $"{json.Version}.json")));
                    addonJson = JsonConvert.DeserializeObject<NovacraftAddonJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, 
                            $"addon.json")));
                    MainDownloader.DownloadAll(null, mainJson, addonJson, online);
                    command = Runner.GenerateCommand(null, mainJson, addonJson, json);
                    break;
                case Runner.Configuration.VersionType.OfficialWithAddonConfig:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                        try { mainJson = MojangFetcher.GetMain(json.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version JSON![/]");
                            return;
                        }
                        addonJson = JsonConvert.DeserializeObject<NovacraftAddonJson>(
                            File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, 
                                $"addon.json")));
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Addon JSON does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(File.ReadAllText(mainJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(null, mainJson, addonJson, online);
                    command = Runner.GenerateCommand(null, mainJson, addonJson, json);
                    break;
                case Runner.Configuration.VersionType.OfficialWithFabricModLoader:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Fabric's Maven repo");
                        try { mainJson = MojangFetcher.GetMain(json.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version JSON![/]");
                            return;
                        }
                        AnsiConsole.WriteLine("[Fabric] Fetching profile JSON");
                        addonJson = FabricFetcher.GetAddon(json.Version);
                        AnsiConsole.WriteLine("[Fabric] Done!");
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonFabricJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Fabric addon JSON does not exist![/]");
                            return;
                        }
                        mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(File.ReadAllText(mainJsonPath));
                        addonJson = JsonConvert.DeserializeObject<NovacraftAddonJson>(File.ReadAllText(addonFabricJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(null, mainJson, addonJson, online);
                    command = Runner.GenerateCommand(null, mainJson, addonJson, json);
                    File.WriteAllText(addonFabricJsonPath, JsonConvert.SerializeObject(addonJson, Formatting.Indented));
                    break;
                case Runner.Configuration.VersionType.OfficialWithForgeModLoader:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Forge's maven repo");
                        try { mainJson = MojangFetcher.GetMain(json.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version JSON![/]");
                            return;
                        }

                        //addonJson = ForgeThingy.GetAddonJson(ForgeThingy.GetLink(json.Version).GetAwaiter().GetResult(), mainJson, online);
                        // Temp 
                        addonJson = ForgeThingy.GetAddonJson(new LauncherConfig.ModPack(), mainJson, online);
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonForgeJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Forge addon JSON does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<NovacraftMainJson>(File.ReadAllText(mainJsonPath));
                        addonJson = JsonConvert.DeserializeObject<NovacraftAddonJson>(File.ReadAllText(addonForgeJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(null, mainJson, addonJson, online);
                    ForgeThingy.RunProcessors(null, mainJson, online);
                    command = Runner.GenerateCommand(null, mainJson, addonJson, json);
                    File.WriteAllText(addonForgeJsonPath, JsonConvert.SerializeObject(addonJson, Formatting.Indented));
                    break;
            }

            if (json.Type == Runner.Configuration.VersionType.CustomVersionFromDir 
                || json.Type == Runner.Configuration.VersionType.CustomWithAddonConfig)
                AnsiConsole.WriteLine("[JSON] Be aware that the author/info may be a lie!");
            AnsiConsole.WriteLine($"[JSON] Version author: {mainJson.Author}");
            AnsiConsole.WriteLine($"[JSON] Version information: {mainJson.Information}");
            if (addonJson != null) {
                AnsiConsole.WriteLine($"[JSON] Addon author: {addonJson.Author}");
                AnsiConsole.WriteLine($"[JSON] Addon information: {addonJson.Information}");
            }
            File.WriteAllText(mainJsonPath, JsonConvert.SerializeObject(mainJson, Formatting.Indented));

            AnsiConsole.WriteLine($"[Runner] Starting minecraft...");
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                WorkingDirectory = FilesManager.Directories.Root,
                FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot, 
                //FileName = Path.Combine(Path.Combine(FilesManager.Directories.GetJavaRoot(mainJson),
                    mainJson.JavaMajor.ToString()), "bin", "java"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = command
            };
            proc.OutputDataReceived += (_, e) => {
                Console.WriteLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) => {
                Console.WriteLine(e.Data);
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            Console.WriteLine("\nPress any key to close...");
            Console.ReadKey();
        }
    }
}