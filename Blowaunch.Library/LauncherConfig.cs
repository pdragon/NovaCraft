// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Numerics;
using Blowaunch.Library.Authentication;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;
using Serilog.Core;
using Serilog;

namespace Blowaunch.Library;

/// <summary>
/// Launcher configuration
/// </summary>
public class LauncherConfig
{
    public class AccountResponse
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
        [JsonProperty("errorMessage")] public string ErrorMessage { get; set; }
    }
    public class VersionClass
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
    }
    public class ModPack
    {
        [JsonProperty("id")] public string? Id { get; set; }
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("version")] public VersionClass Version = new VersionClass() { Id = "1.7.10", Name = "Release 1.7.10" };
        [JsonProperty("jvmArgs")] public string JvmArgs = "";
        [JsonProperty("maxRam")] public string RamMax = "2048";
        [JsonProperty("customResolution")] public bool CustomWindowSize { get; set; }
        [JsonProperty("windowSize")] public Vector2 WindowSize = new(200, 200);
        [JsonProperty("gameArgs")] public string GameArgs = "";
        [JsonProperty("packPath")] public string PackPath = "";
        [JsonProperty("ModProxy")] public string ModProxy = "";
        [JsonProperty("LastStartTime")] public int? Time = 0;
        [JsonProperty("ModProxyVersion")] public ForgeThingy.Versions ModProxyVersion;
        [JsonProperty("forceOffline")] public bool ForceOffline;                                                                   // [ ]
    }

    static VersionClass DefaultVersion = new VersionClass(){Id = "1.7.10", Name = "Realese 1.7.10" };

    //[JsonProperty("windowSize")] public Vector2 WindowSize = new(200, 200);                                                    // [+]
    [JsonProperty("selectedAccountId")] public string SelectedAccountId = "";                                                  // [*]
    [JsonProperty("accounts")] public List<Account> Accounts = new();                                                          // [*]
    //[JsonProperty("customResolution")] public bool CustomWindowSize;                                                           // [+]
    [JsonProperty("showSnapshots")] public bool ShowSnapshots;                                                                 // [+]
    //[JsonProperty("version")] public VersionClass Version = new VersionClass() { Id = "1.7.10", Name = "Realese 1.7.10" };     // [*]
    //[JsonProperty("gameArgs")] public string GameArgs = "";                                                                    // [+]
    //[JsonProperty("jvmArgs")] public string JvmArgs = "";                                                                      // [+]
    //[JsonProperty("maxRam")] public string RamMax = "2048";                                                                    // [+]
    //[JsonProperty("forceOffline")] public bool ForceOffline;                                                                   // [ ]
    [JsonProperty("showAlpha")] public bool ShowAlpha;                                                                         // [+]
    [JsonProperty("showBeta")] public bool ShowBeta;                                                                           // [+]
    [JsonProperty("isDemo")] public bool DemoUser;                                                                             // [ ]
    [JsonProperty("modPacks")] public List<ModPack> ModPacks = new();
    [JsonProperty("selectedModPackId")] public string SelectedModPackId = "";
    //[JsonProperty("forgeInstalledVersions")] public List<string> ForgeInstalledVersions;

    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("blowaunch.log")
        .WriteTo.Console()
        .CreateLogger();

    public static bool SaveConfig(LauncherConfig Config)
    {
        try
        {
            File.WriteAllText("config.json",
            JsonConvert.SerializeObject(
                Config, Formatting.Indented,
           new JsonConverter[] { new StringEnumConverter() }
                ));
            return true;
        }
        catch (Exception e)
        {
            Logger.Error("Unable to save config! {0}", e);
            return false;
        }
    }

    public static void SaveModPackToConfig(LauncherConfig Config, ModPack? modpackConfig)
    {
        var index = Config.ModPacks.FindIndex(mp => mp.Id == modpackConfig?.Id);
        if (index != -1)
        {
            Config.ModPacks[index] = modpackConfig;
        }
        else
        {
            Config.ModPacks.Add(modpackConfig);
        }
    }
}