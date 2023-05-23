using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Blowaunch.Library;
using Blowaunch.Library.Authentication;
using Hardware.Info;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using static Blowaunch.AvaloniaApp.LauncherConfig;
using Panel = Avalonia.Controls.Panel;

namespace Blowaunch.AvaloniaApp.Views;
#pragma warning disable CS8618
#pragma warning disable CS0618
#pragma warning disable CS1998
public class MainWindow : Window
{
    #region UI Elements
    // All fields used
    private Panel _authPanel;
    private Panel _loadingPanel;
    private Panel _progressPanel;
    private ComboBox _versionsCombo;
    private TextBlock _accountName;
    private TextBlock _accountType;
    private ComboBox _accountsCombo;
    private TextBox _usernameMojang;
    private TextBox _passwordMojang;
    private TextBox _usernameCracked;
    private TextBlock _progressInfo;
    private TextBlock _progressFiles;
    private ProgressBar _progressBar;
    private Button _mojangLoginButton;
    private Button _microsoftLoginButton;

    private TextBlock _loadingTextBlock;
    private Panel _modPackPanel;

    // Settings
    private ToggleSwitch _customWindowSize;
    private NumericUpDown _windowWidth;
    private NumericUpDown _windowHeight;
    private TextBox _javaArguments;
    private TextBox _gameArguments;
    private NumericUpDown _ramManual;
    private Slider _ramSlider;
    private ToggleSwitch _showSnaphots;
    private ToggleSwitch _showAlpha;
    private ToggleSwitch _showBeta;
    private ToggleSwitch _forceOffline;
    private ToggleSwitch _minecraftDemo;
    private Button _saveChanges;
    private Button _revertChanges;

    private ToggleSwitch _modPackCustomWindowSize;
    private NumericUpDown _modPackWindowWidth;
    private NumericUpDown _modPackWindowHeight;
    private TextBox _modPackJavaArguments;
    private TextBox _modPackGameArguments;
    private NumericUpDown _modPackRamManual;
    private Slider _modPackRamSlider;
    private TextBox _modPackId;
    private ComboBox _modPacksCombo;
    private ComboBox _modPackVersionsCombo;
    private TextBox _modPackName;
    private ComboBox _modPackModProxyCombo;
    private TextBox _modPackPathInstance;

    #endregion
    #region Other stuff
    /// <summary>
    /// Launcher Configuration
    /// </summary>
    public static LauncherConfig Config = new();
    
    /// <summary>
    /// Serilog Logger
    /// </summary>
    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("blowaunch.log")
        .WriteTo.Console()
        .CreateLogger();
    
    /// <summary>
    /// Is in offline mode?
    /// </summary>
    public static bool OfflineMode;
    
    /// <summary>
    /// Hardware information
    /// </summary>
    private HardwareInfo _info = new();

    /// <summary>
    /// Did the SelectionChanged event was set?
    /// </summary>
    private bool _selectionChanged;

    private Dictionary<int, string> ProxyDict = new Dictionary<int, string>() {
            { 0 , "ModPackVanilla" },
            { 1 , "ModPackForge" },
            { 2 , "ModPackFabric" },
        };

    private class VersionsReturn
    {
        public List<LauncherConfig.VersionClass> Versions = new List<LauncherConfig.VersionClass>();
        public bool IsOffline = false;
    }

    /// <summary>
    /// Show progress actions modal
    /// </summary>
    /// <param name="progressInfo">Info about current process</param>
    /// <param name="progressFiles">Info about current progress stage</param>
    /// <returns></returns>
    private void ProgressModal(string progressInfo, string progressFiles, string? loadingTextBlock = null)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _loadingPanel.IsVisible = true;
            _progressPanel.IsVisible = true;
            _progressBar.IsIndeterminate = true;
            _progressInfo!.Text = progressInfo;
            _progressFiles!.Text = progressFiles;
            if (loadingTextBlock != null)
            {
                _loadingTextBlock!.Text = loadingTextBlock;
            }
        });
    }

    /// <summary>
    /// Close progress actions modal
    /// </summary>
    /// <returns></returns>
    private void ProgressModalDisable()
    {
        Dispatcher.UIThread.InvokeAsync(() => {
            _loadingPanel.IsVisible = false;
            _progressPanel.IsVisible = false;
            _progressBar.IsIndeterminate = false;
        });
    }

    #endregion
    #region Initialization
    /// <summary>
    /// Initialize everything
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        InitializeFields();
        _loadingPanel!.IsVisible = true;
        _progressPanel!.IsVisible = true;
        _progressBar!.IsIndeterminate = true;

        new Thread(async () => {
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading hardware info...";
                _progressFiles!.Text = "Step 1 out of 5";
            });
            _info.RefreshMemoryList();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading configuration...";
                _progressFiles!.Text = "Step 2 out of 5";
            });
            await LoadConfig();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading versions...";
                _progressFiles!.Text = "Step 3 out of 5";
            });
            await LoadVersions();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Validating accounts...";
                _progressFiles!.Text = "Step 4 out of 5";
            });
            ValidateAccounts();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading settings and accounts...";
                _progressFiles!.Text = "Step 5 out of 5";
            });
            await Dispatcher.UIThread.InvokeAsync(() => {
                LoadSettings();
                ReloadAccounts();
                ReloadModPacks();
                _loadingPanel.IsVisible = false;
                _progressPanel.IsVisible = false;
                _progressBar.IsIndeterminate = false;
            });
        }).Start();
    }
    
    /// <summary>
    /// Initialize components
    /// </summary>
    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);
    #endregion
    #region CheckForInternet()
    /// <summary>
    /// Check for internet connection
    /// </summary>
    /// <param name="timeoutMs">Timeout (in millis)</param>
    /// <returns>Boolean value</returns>
    private static bool CheckForInternet(int timeoutMs = 5000)
    {
        try {
            HttpClient Client = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };
            var request = Client.GetStringAsync("https://google.com");
            request.Wait();
            string response = request.Result;

            //var request = (HttpWebRequest)WebRequest.Create("https://google.com");
            //request.KeepAlive = false;
            //request.Timeout = timeoutMs;
            //using var response = (HttpWebResponse)request.GetResponse();
            return true;
        } catch { return false; }
    }
    #endregion
    #region InitializeFields()
    /// <summary>
    /// Initialize fields used
    /// </summary>
    private void InitializeFields()
    {
        _loadingPanel = this.FindControl<Panel>("Loading");
        _authPanel = this.FindControl<Panel>("Authentication");
        _versionsCombo = this.FindControl<ComboBox>("Versions");
        _accountsCombo = this.FindControl<ComboBox>("Accounts");
        _progressPanel = this.FindControl<Panel>("ProgressPanel");
        _accountName = this.FindControl<TextBlock>("AccountName");
        _accountType = this.FindControl<TextBlock>("AccountType");
        _usernameMojang = this.FindControl<TextBox>("UsernameMojang");
        _passwordMojang = this.FindControl<TextBox>("PasswordMojang");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _progressInfo = this.FindControl<TextBlock>("ProgressInfo");
        _progressFiles = this.FindControl<TextBlock>("ProgressFiles");
        _mojangLoginButton = this.FindControl<Button>("MojangButton");
        _usernameCracked = this.FindControl<TextBox>("UsernameCracked");
        _microsoftLoginButton = this.FindControl<Button>("LoginButton");
        _customWindowSize = this.FindControl<ToggleSwitch>("CustomWindowSize");
        _showSnaphots = this.FindControl<ToggleSwitch>("ShowSnapshots");
        _showAlpha = this.FindControl<ToggleSwitch>("ShowAlpha");
        _showBeta = this.FindControl<ToggleSwitch>("ShowBeta");
        _forceOffline = this.FindControl<ToggleSwitch>("ForceOffline");
        _minecraftDemo = this.FindControl<ToggleSwitch>("MinecraftDemo");
        _windowWidth = this.FindControl<NumericUpDown>("WindowWidth");
        _windowHeight = this.FindControl<NumericUpDown>("WindowHeight");
        _ramManual = this.FindControl<NumericUpDown>("RamManual");
        _javaArguments = this.FindControl<TextBox>("JavaArguments");
        _gameArguments = this.FindControl<TextBox>("GameArguments");
        _saveChanges = this.FindControl<Button>("SaveChanges");
        _revertChanges = this.FindControl<Button>("RevertChanges");
        _ramSlider = this.FindControl<Slider>("RamSlider");
        _loadingTextBlock = this.FindControl<TextBlock>("LoadingTextBlock");
        _modPackPanel = this.FindControl<Panel>("ModPackAdd");

        _modPackCustomWindowSize = this.FindControl<ToggleSwitch>("ModPackCustomWindowSize");
        _modPackWindowWidth = this.FindControl<NumericUpDown>("ModPackWindowWidth");
        _modPackWindowHeight = this.FindControl<NumericUpDown>("ModPackWindowHeight");
        _modPackRamManual = this.FindControl<NumericUpDown>("ModPackRamManual");
        _modPackJavaArguments = this.FindControl<TextBox>("ModPackJavaArguments");
        _modPackGameArguments = this.FindControl<TextBox>("ModPackGameArguments");
        _modPackId = this.FindControl<TextBox>("ModPackId");
        _modPackRamSlider = this.FindControl<Slider>("ModPackRamSlider");
        _modPacksCombo = this.FindControl<ComboBox>("ModPacks");
        _modPackVersionsCombo = this.FindControl<ComboBox>("ModPackVersions");
        _modPackName = this.FindControl<TextBox>("ModPackName");
        _modPackModProxyCombo = this.FindControl<ComboBox>("ModPackModProxyCombo");
        _modPackPathInstance = this.FindControl<TextBox>("ModPackPathInstance");

        _ramManual.ValueChanged += (_, e) => {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_ramSlider.Value == _ramManual.Value)
                return;
            if (e.NewValue > _ramSlider.Maximum)
                _ramManual.Value = _ramSlider.Maximum;
            else if (e.NewValue < 0)
                _ramManual.Value = 0;
            
            _ramSlider.Value = e.NewValue;
        };

        _modPackRamManual.ValueChanged += (_, e) => {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_modPackRamSlider.Value == _modPackRamManual.Value)
                return;
            if (e.NewValue > _modPackRamSlider.Maximum)
                _modPackRamManual.Value = _modPackRamSlider.Maximum;
            else if (e.NewValue < 0)
                _modPackRamManual.Value = 0;

            _modPackRamSlider.Value = e.NewValue;
        };

        _windowWidth.ValueChanged += (_, e) => {
            if (e.NewValue < 0)
                _windowWidth.Value = 0;
        };
        
        _windowHeight.ValueChanged += (_, e) => {
            if (e.NewValue < 0)
                _windowHeight.Value = 0;
        };

        _modPackRamSlider.PropertyChanged += (_, _) => {
            if (_modPackRamSlider.Value % 1 != 0)
                _modPackRamSlider.Value = Math.Floor(_modPackRamSlider.Value);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_modPackRamSlider.Value == _modPackRamManual.Value)
                return;
            _modPackRamManual.Value = _modPackRamSlider.Value;
        };

        _modPacksCombo.SelectionChanged += (_, e) => {
            var modPack = (LauncherConfig.ModPack?)_modPacksCombo.SelectedItem;
            if (modPack == null || modPack.Name == "New Instance") return;
            Config.SelectedModPackId = modPack.Id ?? "";
            SaveConfig();
        };
    }
    #endregion
    #region LoadVersions()
    /// <summary>
    /// Load Minecraft versions
    /// </summary>
    private async Task LoadVersions()
    {
        /*
        Logger.Information("Loading versions...");
        var versions = new List<LauncherConfig.VersionClass>();
        if (CheckForInternet()) {
            Logger.Information("Internet available, fetching");
            var json = MojangFetcher.GetVersions();
            foreach (var i in json.Versions) {
                try {
                    var sb = new StringBuilder();
                    switch (i.Type) {
                        case BlowaunchMainJson.JsonType.release:
                            sb.Append("Release ");
                            break;
                        case BlowaunchMainJson.JsonType.snapshot:
                            if (!Config.ShowSnapshots) continue;
                            sb.Append("Snapshot ");
                            break;
                        case BlowaunchMainJson.JsonType.old_beta:
                            if (!Config.ShowBeta) continue;
                            sb.Append("Beta ");
                            break;
                        case BlowaunchMainJson.JsonType.old_alpha:
                            if (!Config.ShowAlpha) continue;
                            sb.Append("Alpha ");
                            break;
                    }

                    sb.Append(i.Id);
                    var item = new LauncherConfig.VersionClass 
                        { Name = sb.ToString(), Id = i.Id };
                    versions.Add(item);
                } catch (Exception e) {
                    Logger.Error($"Unable to load {i.Id}! {0}", e);
                }
            }
        } else {
            Logger.Warning("Internet unavailable!");
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Offline mode enabled - " +
                                         "integrity checks would be " +
                                         "skipped.",
                        ContentTitle = "No internet connection!"
                    });
                msBoxStandardWindow.Show();
            });
            OfflineMode = true;
        }

        
        Logger.Information("Loading custom versions...");
        foreach (var i in Directory.GetDirectories(
                     FilesManager.Directories.VersionsRoot)) {
            var name = Path.GetFileName(i);
            if (versions.Any(x => x.Id == name))
                continue;
            
            Logger.Information($"Processing {name}");
            try {
                var jsonPath = Path.Combine(i + "\\", $"{name}.json");
                var json = File.ReadAllText(jsonPath);
                dynamic d = JObject.Parse(json);
                if (MojangMainJson.IsMojangJson(d))
                    json = JsonConvert.SerializeObject(
                        BlowaunchMainJson.MojangToBlowaunch(
                            JsonConvert.DeserializeObject
                                <MojangMainJson>(json)));
                var actualJson = JsonConvert.DeserializeObject
                    <BlowaunchMainJson>(json);
                var sb = new StringBuilder();
                switch (actualJson!.Type) {
                    case BlowaunchMainJson.JsonType.release:
                        sb.Append("Release ");
                        break;
                    case BlowaunchMainJson.JsonType.snapshot:
                        if (!Config.ShowSnapshots) continue;
                        sb.Append("Snapshot ");
                        break;
                    case BlowaunchMainJson.JsonType.old_beta:
                        if (!Config.ShowBeta) continue;
                        sb.Append("Beta ");
                        break;
                    case BlowaunchMainJson.JsonType.old_alpha:
                        if (!Config.ShowAlpha) continue;
                        sb.Append("Alpha ");
                        break;
                }

                sb.Append(actualJson.Version);
                var item = new LauncherConfig.VersionClass
                    { Name = sb.ToString(), Id = name };
                versions.Add(item);
            } catch (Exception e) {
                Logger.Error("Unable to load the version! {0}", e);
            }
        }
        */
        var versions = GetVersions();
        if (versions.IsOffline)
        {
            Logger.Warning("Internet unavailable!");
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Offline mode enabled - " +
                                         "integrity checks would be " +
                                         "skipped.",
                        ContentTitle = "No internet connection!"
                    });
                msBoxStandardWindow.Show();
            });
        }
        var index = versions.Versions.FindIndex(
            x => x.Id == Config.Version.Id 
                 && x.Name == Config.Version.Name);
       
        
        await Dispatcher.UIThread.InvokeAsync(() => {
            _versionsCombo.Items = versions.Versions;
            _versionsCombo.SelectedIndex = index;
            if (_selectionChanged) return;
            _versionsCombo.SelectionChanged += (_, e) => {
                if (e.AddedItems.Count == 0) return;
                Config.Version = (e.AddedItems[0] 
                    as LauncherConfig.VersionClass)!;
                SaveConfig();
            };
            _selectionChanged = true;
        });
    }

    private VersionsReturn GetVersions()
    {
        Logger.Information("Loading versions...");
        var versions = new List<LauncherConfig.VersionClass>();
        if (CheckForInternet())
        {
            Logger.Information("Internet available, fetching");
            var json = MojangFetcher.GetVersions();
            foreach (var i in json.Versions)
            {
                try
                {
                    var sb = new StringBuilder();
                    switch (i.Type)
                    {
                        case BlowaunchMainJson.JsonType.release:
                            sb.Append("Release ");
                            break;
                        case BlowaunchMainJson.JsonType.snapshot:
                            if (!Config.ShowSnapshots) continue;
                            sb.Append("Snapshot ");
                            break;
                        case BlowaunchMainJson.JsonType.old_beta:
                            if (!Config.ShowBeta) continue;
                            sb.Append("Beta ");
                            break;
                        case BlowaunchMainJson.JsonType.old_alpha:
                            if (!Config.ShowAlpha) continue;
                            sb.Append("Alpha ");
                            break;
                    }

                    sb.Append(i.Id);
                    var item = new LauncherConfig.VersionClass
                    { Name = sb.ToString(), Id = i.Id };
                    versions.Add(item);
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to load {i.Id}! {0}", e);
                }
            }
        }
        else
        {
            Logger.Warning("Internet unavailable!");
            /*
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Offline mode enabled - " +
                                         "integrity checks would be " +
                                         "skipped.",
                        ContentTitle = "No internet connection!"
                    });
                msBoxStandardWindow.Show();
            });
            */
            OfflineMode = true;
        }


        Logger.Information("Loading custom versions...");
        foreach (var i in Directory.GetDirectories(
                     FilesManager.Directories.VersionsRoot))
        {
            var name = Path.GetFileName(i);
            if (versions.Any(x => x.Id == name))
                continue;

            Logger.Information($"Processing {name}");
            try
            {
                string? dirName = i;
                string jsonPath;
                if (ForgeJson.IsForgeJSONFilename(name))
                {
                    continue;
                    //dirName = i.Split("-forge-")[0];
                    //jsonPath = Path.Combine(dirName + "\\", $"{name.Split("-forge-")[0]}.json");
                }
                else
                {
                    jsonPath = Path.Combine(dirName + "\\", $"{name}.json");
                }
                var json = File.ReadAllText(jsonPath);
                dynamic d = JObject.Parse(json);
                // TODO: check for forge json
                if (MojangMainJson.IsNotBlowaunchJson(d))
                    json = JsonConvert.SerializeObject(
                        BlowaunchMainJson.MojangToBlowaunch(
                            JsonConvert.DeserializeObject
                                <MojangMainJson>(json)));
                var actualJson = JsonConvert.DeserializeObject
                    <BlowaunchMainJson>(json);
                var sb = new StringBuilder();
                switch (actualJson!.Type)
                {
                    case BlowaunchMainJson.JsonType.release:
                        sb.Append("Release ");
                        break;
                    case BlowaunchMainJson.JsonType.snapshot:
                        if (!Config.ShowSnapshots) continue;
                        sb.Append("Snapshot ");
                        break;
                    case BlowaunchMainJson.JsonType.old_beta:
                        if (!Config.ShowBeta) continue;
                        sb.Append("Beta ");
                        break;
                    case BlowaunchMainJson.JsonType.old_alpha:
                        if (!Config.ShowAlpha) continue;
                        sb.Append("Alpha ");
                        break;
                }

                sb.Append(actualJson.Version);
                var item = new LauncherConfig.VersionClass
                { Name = sb.ToString(), Id = name };
                versions.Add(item);
            }
            catch (Exception e)
            {
                Logger.Error("Unable to load the version! {0}", e);
            }
        }
        return new VersionsReturn()
        {
            Versions = versions,
            IsOffline = OfflineMode

        };
    }
    #endregion
    #region Configuration
    /// <summary>
    /// Load configuration file
    /// </summary>
    private async Task LoadConfig()
    {
        Logger.Information("Loading configuration...");
        if (!File.Exists("config.json")) {
            Logger.Information("Not found, creating new one");
            /*
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Blowaunch uses it's own " +
                                         "JSON file format, so " +
                                         "you cannot use the Blowaunch " +
                                         "directory in other launcher!",
                        ContentTitle = "Warning!"
                    });
                msBoxStandardWindow.Show();
            });
            */
            SaveConfig();
        } else {
            try {
                Config = JsonConvert.DeserializeObject
                    <LauncherConfig>(File.ReadAllText(
                        "config.json"))!;
            } catch (Exception e) {
                Logger.Error("Unable to load config! {0}", e);
                /*
                await Dispatcher.UIThread.InvokeAsync(() => {
                    var msBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                            Icon = MessageBox.Avalonia.Enums.Icon.Error,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = "Blowaunch is unable to load " +
                                             "the configuration file.\n" +
                                             "Settings were not loaded.",
                            ContentTitle = "An error occured!"
                        });
                    msBoxStandardWindow.Show();
                });
                */
            }
        }
    }

    /// <summary>
    /// Save configuration file
    /// </summary>
    private void SaveConfig()
    {
        try { 
            File.WriteAllText("config.json",
            JsonConvert.SerializeObject(
                Config, Formatting.Indented,
           new JsonConverter[] { new StringEnumConverter() }
                ));
        }
        catch (Exception e) {
            Logger.Error("Unable to save config! {0}", e);
            /*
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "Blowaunch is unable to save " +
                                     "the configuration file.\n" +
                                     "Settings would be reset " +
                                     "when you close and open " +
                                     "Blowaunch again.",
                    ContentTitle = "An error occured!"
                });
            msBoxStandardWindow.Show();
            */
        }
    }
    #endregion
    #region Accounts
    private void ValidateAccounts()
    {
        var accounts = new List<Account>();
        foreach (var acc in Config.Accounts) {
            switch (acc.Type) {
                case Account.AuthType.Microsoft:
                    if (DateTime.Now <= acc.ValidUntil) {
                        var clone = acc;
                        try {
                            Library.Authentication.Microsoft.Refresh(ref clone);
                            accounts.Add(clone);
                        } catch (Exception e) {
                            Logger.Warning($"Microsoft account {acc.Name} " +
                                           $"cannot be refreshed, deleting...", e);
                        }
                    } else accounts.Add(acc);
                    break;
                case Account.AuthType.Mojang:
                    if (!Mojang.Validate(acc)) {
                        var clone = acc;
                        try {
                            Mojang.Refresh(ref clone);
                            accounts.Add(clone);
                        } catch (Exception e) {
                            Logger.Warning($"Microsoft account {acc.Name} " +
                                           $"cannot be refreshed, deleting...", e);
                        }
                    } else accounts.Add(acc);
                    break;
                case Account.AuthType.None:
                    accounts.Add(acc);
                    break;
            }
        }

        Config.Accounts = accounts;
        SaveConfig();
    }
    
    /// <summary>
    /// Reload accounts combo
    /// and textblocks
    /// </summary>
    private void ReloadAccounts()
    {
        Logger.Information("Loading accounts...");
        _accountsCombo.Items = Config.Accounts.ToList();
        var account = Config.Accounts.Where(x => 
                x.Id == Config.SelectedAccountId)
            .ToList();
        if (account.Count == 0) {
            _accountName.Text = "[No Account]";
            _accountType.Text = "No authentication";
            return;
        }
        
        _accountsCombo.SelectedItem = account;
        _accountName.Text = account[0].Name;
        switch (account[0].Type) {
            case Account.AuthType.Microsoft:
                _accountType.Text = "Microsoft Account";
                break;
            case Account.AuthType.Mojang:
                _accountType.Text = "Mojang Account";
                break;
            case Account.AuthType.None:
                _accountType.Text = "No authentication";
                break;
        }
    }

    private void ReloadModPacks()
    {
        Logger.Information("Loading modpacks...");
        var modPacks = Config.ModPacks;
        //modPacks["New Instance"] = new() {Name = "New Instance" };
        List<LauncherConfig.ModPack> modPacksList = new();
        modPacksList.Add(new(){ Name = "New Instance", Id = "New Instance" });
        modPacksList.AddRange(Config.ModPacks.ToList());
        _modPacksCombo.Items = modPacksList; // Config.ModPacks.ToList();

        var modpack = Config.ModPacks.Where(x =>
                x.Id == Config.SelectedModPackId)
            .FirstOrDefault();
        _modPacksCombo.SelectedItem = modpack;

    }



    /// <summary>
    /// Delete selected account
    /// </summary>
    public void DeleteAccount(object? sender, RoutedEventArgs e)
    {
        var item = _accountsCombo.SelectedItem as Account;
        if (Config.SelectedAccountId == item!.Id)
            Config.SelectedAccountId = "";
        _accountsCombo.SelectedIndex = 0;
        Config.Accounts.Remove(item);
        SaveConfig(); ReloadAccounts();
    }

    /// <summary>
    /// Select account
    /// </summary>
    public void SelectAccount(object? sender, RoutedEventArgs e)
    {
        if (_accountsCombo.SelectedItem is not Account item) {
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "Select an account first!",
                    ContentTitle = "Error"
                });
            msBoxStandardWindow.Show();
            return;
        }
        Config.SelectedAccountId = item.Id;
        //Config.Account.Name = item.Name;
        SaveConfig(); ReloadAccounts();
        _authPanel.IsVisible = false;
    }
    
    /// <summary>
    /// Login into Cracked account
    /// </summary>
    public void CrackedLogin(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Adding cracked account...");
        Config.Accounts.Add(new Account {
           Type = Account.AuthType.None,
           Name = _usernameCracked.Text,
           Id = Guid.NewGuid().ToString()
        });
        Logger.Information("Successfully logged in!");
        SaveConfig(); ReloadAccounts();
        
        var msBoxStandardWindow = MessageBoxManager
            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                Icon = MessageBox.Avalonia.Enums.Icon.Success,
                ButtonDefinitions = ButtonEnum.Ok,
                ContentMessage = "Successfully created a Cracked account!",
                ContentTitle = "Success"
            });
        msBoxStandardWindow.Show();
    }
    
    /// <summary>
    /// Login into Microsoft account
    /// </summary>
    public void MicrosoftLogin(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Starting Microsoft OAuth2 listener...");
        try {
            Library.Authentication.Microsoft.OpenLoginPage();
            Library.Authentication.Microsoft.StartListener(
                async acc => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        _microsoftLoginButton.IsVisible = true;
                        _progressPanel.IsVisible = false;
                        _progressInfo.Text = "";
                        acc.Id = Guid.NewGuid().ToString();
                        Config.Accounts.Add(acc);
        
                        Logger.Information("Successfully logged in!");
                        SaveConfig(); ReloadAccounts();
        
                        var msBoxStandardWindow1 = MessageBoxManager
                            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                                Icon = MessageBox.Avalonia.Enums.Icon.Success,
                                ButtonDefinitions = ButtonEnum.Ok,
                                ContentMessage = "Successfully logged into your Minecraft account!",
                                ContentTitle = "Success"
                            });
                        msBoxStandardWindow1.Show();
                    }), async ex => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        _microsoftLoginButton.IsVisible = true;
                        _progressPanel.IsVisible = false;
                        _progressInfo.Text = "";
                        var msBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                                Icon = MessageBox.Avalonia.Enums.Icon.Error,
                                ButtonDefinitions = ButtonEnum.Ok,
                                ContentMessage = ex.Message,
                            ContentTitle = ex.GetType().Name
                        });
                        msBoxStandardWindow.Show();
                }), async (str, c) => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        if (c == -1)
                            _progressBar.IsIndeterminate = true;
                        else {
                            _progressFiles.Text = $"Step {c + 1} out of 5";
                            _progressBar.IsIndeterminate = false;
                            _progressBar.Value = c;
                        }
                        _progressInfo.Text = str;
                    }));
            _microsoftLoginButton.IsVisible = false;
            _progressFiles.Text = "Step 1 out of 5";
            _progressPanel.IsVisible = true;
            _progressBar.Maximum = 4;
        } catch (Exception ex) {
            Logger.Error("An error occured: {0}", ex);
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = ex.Message,
                    ContentTitle = ex.GetType().Name
                });
            msBoxStandardWindow.Show();
        }
    }

    /// <summary>
    /// Login into Mojang account
    /// </summary>
    public void MojangLogin(object? sender, RoutedEventArgs e)
    {
        _mojangLoginButton.IsVisible = false;
        _progressBar.IsIndeterminate = true;
        _progressInfo.Text = "Logging in...";
        _progressFiles.Text = "Step 1 out of 1";
        _progressPanel.IsVisible = true;
        new Thread(async () => {
            try {
                Logger.Information("Adding Mojang account...");
                var account = Mojang.Login(_usernameMojang.Text,
                    _passwordMojang.Text);
                account.Id = Guid.NewGuid().ToString();
                Config.Accounts.Add(account);
            
                Logger.Information("Successfully logged in!");
                await Dispatcher.UIThread.InvokeAsync(() => {
                    SaveConfig(); ReloadAccounts();
                    _progressBar.IsIndeterminate = false;
                    _mojangLoginButton.IsVisible = true;
                    _progressPanel.IsVisible = false;
                    _progressInfo.Text = "";
                    var msBoxStandardWindow1 = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                            Icon = MessageBox.Avalonia.Enums.Icon.Success,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = "Successfully logged into your Minecraft account!",
                            ContentTitle = "Success"
                        });
                    msBoxStandardWindow1.Show();
                });
            } catch (Exception ex) {
                Logger.Error("An error occured: {0}", ex);
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _progressBar.IsIndeterminate = false;
                    _mojangLoginButton.IsVisible = true;
                    _progressPanel.IsVisible = false;
                    _progressInfo.Text = "";
                    var msBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                            Icon = MessageBox.Avalonia.Enums.Icon.Error,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = ex.Message,
                            ContentTitle = ex.GetType().Name
                        });
                    msBoxStandardWindow.Show();
                });
            }
        }).Start();
    }
    #endregion
    #region Settings
    /// <summary>
    /// Show settings in the UI
    /// </summary>
    private void LoadSettings()
    {
        Logger.Information("Loading settings...");
        _customWindowSize.IsChecked = Config.CustomWindowSize;
        _windowWidth.Value = Config.WindowSize.X;
        _windowHeight.Value = Config.WindowSize.Y;
        _javaArguments.Text = Config.JvmArgs;
        _gameArguments.Text = Config.GameArgs;
        _ramSlider.Maximum = _info.MemoryList.Sum(
            x => (long)x.Capacity) / 1000000;
        _ramManual.Value = int.Parse(Config.RamMax);
        _ramSlider.Value = int.Parse(Config.RamMax);
        _showSnaphots.IsChecked = Config.ShowSnapshots;
        _showAlpha.IsChecked = Config.ShowAlpha;
        _showBeta.IsChecked = Config.ShowBeta;
        _forceOffline.IsChecked = Config.ForceOffline;
        _minecraftDemo.IsChecked = Config.DemoUser;

        _modPackRamSlider.Maximum = _info.MemoryList.Sum(
            x => (long)x.Capacity) / 1000000;
        _modPackRamManual.Value = int.Parse(Config.RamMax);
        _modPackRamSlider.Value = int.Parse(Config.RamMax);

        if (Config.ForceOffline)
            OfflineMode = true;
    }
    
    /// <summary>
    /// Reloads settings
    /// </summary>
    public void RevertChanges(object? sender, RoutedEventArgs e)
        => LoadSettings();

    /// <summary>
    /// Save settings
    /// </summary>
    public void SaveChanges(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Saving settings...");
        Config.CustomWindowSize = _customWindowSize.IsChecked!.Value;
        Config.WindowSize = new Vector2(
            (int)_windowWidth.Value,
            (int)_windowHeight.Value);
        Config.JvmArgs = _javaArguments.Text;
        Config.GameArgs = _gameArguments.Text;
        Config.RamMax = _ramManual.Value.ToString(
            CultureInfo.InvariantCulture);
        Config.ShowSnapshots = _showSnaphots.IsChecked!.Value;
        Config.ShowAlpha = _showAlpha.IsChecked!.Value;
        Config.ShowBeta = _showBeta.IsChecked!.Value;
        Config.ForceOffline = _forceOffline.IsChecked!.Value;
        Config.DemoUser = _minecraftDemo.IsChecked!.Value;
        SaveConfig();
        
        _loadingPanel.IsVisible = true;
        _progressPanel.IsVisible = true;
        _progressBar.IsIndeterminate = true;

        new Thread(async () => {
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo.Text = "Loading versions...";
                _progressFiles.Text = "Step 1 out of 1";
            });
            await LoadVersions();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _loadingPanel.IsVisible = false;
                _progressPanel.IsVisible = false;
                _progressBar.IsIndeterminate = false;
            });
        }).Start();
    }

    /// <summary>
    /// Save instance
    /// </summary>
    public void ModPackSaveChanges(object? sender, RoutedEventArgs e)
    {
        string id = _modPackId.Text;
        if(id == "")
        {
            id = Guid.NewGuid().ToString();
            Logger.Information("Creating new instance");
        }
        LauncherConfig.ModPack modpackConfig = new LauncherConfig.ModPack();
        Logger.Information("Saving instance settings...");
        modpackConfig.CustomWindowSize = _customWindowSize.IsChecked!.Value;
        modpackConfig.WindowSize = new Vector2(
            (int)_modPackWindowWidth.Value,
            (int)_modPackWindowHeight.Value);
        modpackConfig.JvmArgs = _modPackJavaArguments.Text;
        modpackConfig.GameArgs = _modPackGameArguments.Text;
        modpackConfig.RamMax = _modPackRamManual.Value.ToString(CultureInfo.InvariantCulture);
        if (_modPackVersionsCombo.SelectedItem is LauncherConfig.VersionClass)
        {
            modpackConfig.Version = (LauncherConfig.VersionClass)_modPackVersionsCombo.SelectedItem;
        }
        modpackConfig.Id = id;
        modpackConfig.Name = _modPackName.Text;
        modpackConfig.RamMax = _modPackRamSlider.Value.ToString(CultureInfo.InvariantCulture);
        modpackConfig.PackPath = _modPackPathInstance.Text;
        //config.ShowSnapshots = _showSnaphots.IsChecked!.Value;
        //config.ShowAlpha = _showAlpha.IsChecked!.Value;
        //config.ShowBeta = _showBeta.IsChecked!.Value;
        //config.ForceOffline = _forceOffline.IsChecked!.Value;
        //config.DemoUser = _minecraftDemo.IsChecked!.Value;


        if (_modPackModProxyCombo.SelectedIndex > -1)
        {
            modpackConfig.ModProxy = ProxyDict[_modPackModProxyCombo.SelectedIndex];
        }
        var index = Config.ModPacks.FindIndex(mp => mp.Id == modpackConfig.Id);
        if (index != -1)
        {
            Config.ModPacks[index] = modpackConfig;
        }
        else
        {
            Config.ModPacks.Add(modpackConfig);
        }
        if (modpackConfig.Name == null || modpackConfig.Name == "")
        {
            ShowMessage("Name can't be empty", "Error occured");
            return;
        }
        SaveConfig();
        ReloadModPacks();



        _loadingPanel.IsVisible = true;
        _progressPanel.IsVisible = true;
        _progressBar.IsIndeterminate = true;

        new Thread(async () => {
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo.Text = "Loading versions...";
                _progressFiles.Text = "Step 1 out of 1";
            });
            await LoadVersions();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _loadingPanel.IsVisible = false;
                _progressPanel.IsVisible = false;
                _progressBar.IsIndeterminate = false;
                _modPackPanel.IsVisible = false;
            });
        }).Start();
    }

    /// <summary>
    /// Reset settings
    /// </summary>
    public void ResetSettings(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Resetting settings...");
        var conf = new LauncherConfig();
        Config.CustomWindowSize = conf.CustomWindowSize;
        Config.WindowSize = conf.WindowSize;
        Config.JvmArgs = conf.JvmArgs;
        Config.GameArgs = conf.GameArgs;
        Config.RamMax = conf.RamMax;
        Config.ShowSnapshots = conf.ShowSnapshots;
        Config.ShowAlpha = conf.ShowAlpha;
        Config.ShowBeta = conf.ShowBeta;
        Config.ForceOffline = conf.ForceOffline;
        Config.DemoUser = conf.DemoUser;
        SaveConfig(); LoadSettings();
    }
    #endregion
    #region Events
    /// <summary>
    /// Close authentication panel
    /// </summary>
    public void CloseAuthPanel(object? sender, RoutedEventArgs e)
        => _authPanel.IsVisible = false;
        
    /// <summary>
    /// Open authentication panel
    /// </summary>
    public void OpenAuthPanel(object? sender, RoutedEventArgs e)
        => _authPanel.IsVisible = true;
    
    /// <summary>
    /// Open root directory
    /// </summary>
    public void OpenDirectory(object? sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo {
            FileName = FilesManager.Directories.Root,
            UseShellExecute = true,
            Verb = "open"
        });
    public void AddModpack(object? sender, RoutedEventArgs e)
         => Process.Start(new ProcessStartInfo
         {
             FileName = FilesManager.Directories.Root,
             UseShellExecute = true,
             Verb = "open"
         });

    /// <summary>
    /// Close Modpack panel
    /// </summary>
    public void CloseModPackPanel(object? sender, RoutedEventArgs e)
        => _modPackPanel.IsVisible = false;

    /// <summary>
    /// Open Modpack panel
    /// </summary>
    public void OpenModpackPanel(object? sender, RoutedEventArgs e)
    {

        if (_modPacksCombo != null && _modPacksCombo.SelectedItem != null)
        {
            LauncherConfig.ModPack modPack = Config.ModPacks.Find(mp => mp.Id == ((LauncherConfig.ModPack)_modPacksCombo.SelectedItem).Id) ?? new LauncherConfig.ModPack();
            if(modPack.Id == null)
            {
                ShowMessage("Something wrong", "Error occured");
            }
            int index = 1;
            string? id = ((LauncherConfig.ModPack)_modPacksCombo.SelectedItem).Id == null ? Guid.NewGuid().ToString() : modPack.Id;
            _modPackId.Text = id == "New Instance" ? "" : id;
            if (modPack.ModProxy != "")
            {
                var proxyIndex = ProxyDict.FirstOrDefault(x => x.Value == modPack.ModProxy).Key;
                if(proxyIndex != -1){
                    _modPackModProxyCombo.SelectedIndex = proxyIndex;
                }
            }
            _modPackName.Text =  modPack.Name;
            _modPackRamSlider.Value = Convert.ToDouble(modPack.RamMax);
            _modPackPathInstance.Text = modPack.PackPath;
            //_modPackVersionsCombo.Items.F = (LauncherConfig.VersionClass?)modPack.Version;
            //LauncherConfig.VersionClass? a = _modPackVersionsCombo.FirstOrDefault(modPack.Version);
            //_modPackVersionsCombo.SelectedItem = modPack.Version;
            if (id == "")
            {
                return;
            }
            _modPackPanel.IsVisible = true;
            var versionsClass = GetVersions();
            if (versionsClass != null)
            {
                if (id != "")
                {
                    index = versionsClass.Versions.FindIndex(
                        x => x.Id == modPack.Version.Id
                             && x.Name == modPack.Version.Name);
                }

                Dispatcher.UIThread.InvokeAsync(() => {
                    _modPackVersionsCombo.Items = versionsClass.Versions;
                    _modPackVersionsCombo.SelectedIndex = index;
                    //_modPackVersionsCombo.SelectedItem = modPack.Version;
                    if (_selectionChanged) return;

                    //_modPackVersionsCombo.SelectionChanged += (_, e) => {
                    //    if (e.AddedItems.Count == 0) return;
                    //    Config.Version = (e.AddedItems[0]
                    //        as LauncherConfig.VersionClass)!;
                    //    SaveConfig();
                    //};
                    //_selectionChanged = true;
                });
            }
        }
    }
        
    #endregion
    #region Runner

    public void RunMinecraft(object? sender, RoutedEventArgs e)
    {
        if (_progressPanel.IsVisible) {
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "An operation is active at the time!",
                    ContentTitle = "Error"
                });
            msBoxStandardWindow.Show();
            return;
        }
        bool online = true;
        new Thread(async () => {
            ProgressModal("Loading data...", "0 % done", "Downloading minecraft client");
            await LoadData(online);

            ProgressModal("Starting Minecraft client " + Config.Version, "0 % done");
            await RunMinecraftProgress(online);
            ProgressModalDisable();
        }).Start();
    }

    private async Task LoadData(bool online)
    {
        var currentModpack = (LauncherConfig.ModPack?)_modPacksCombo.SelectedItem;
        if (currentModpack == null)
        {
            ShowMessage("Please select modPack", "Error");
            return;
        }
        BlowaunchMainJson main = (MojangFetcher.GetMain(currentModpack.Version.Id));
        ProgressModal("Loading client", "please wait");
        FilesManager.DownloadClient(main, online);
        AnsiConsole.MarkupLine($"[grey] checking and downdloadeing needed libraries " + $"[/]");
        int itemsDownloaded = 0;
        
        foreach (BlowaunchMainJson.JsonLibrary library in main.Libraries)
        {
            AnsiConsole.MarkupLine($"[grey] library {library.Name} " + $"[/]");
            itemsDownloaded++;
            ProgressModal("Loading libraries...", (int)((float)itemsDownloaded / main.Libraries.Length * 100) + " %");
            FilesManager.DownloadLibrary(library, currentModpack.Version.Id, online);
        }

        var MojangJson = FilesManager.LoadMojangAssets(currentModpack.Version.Id, true, main);
        BlowaunchAssetsJson assetsJson = BlowaunchAssetsJson.MojangToBlowaunch(MojangJson);

        for(int i = 0; i < assetsJson.Assets.Length; i++)
        {
            FilesManager.DownloadAsset(assetsJson.Assets[i], online);
            ProgressModal("Loading assets...", i + " from " + assetsJson.Assets.Length + "("+(int)((float)i / assetsJson.Assets.Length * 100) + " %)");
        }
        AnsiConsole.MarkupLine($"[yellow] downoading complete " + $"[/]");
    }

    private async Task RunMinecraftProgress(bool online)
    {
        var User = Config.Accounts.Where(a => a.Id == Config.SelectedAccountId).FirstOrDefault();
        if (User == null)
        {
            // throw error
        }
        else
        {
            var currentModpack =  (LauncherConfig.ModPack?)_modPacksCombo.SelectedItem;
            if (currentModpack == null || currentModpack.Id == null || currentModpack.Id == "") {
                ShowMessage("Error","Error");
                return;
            }
            // TODO: Create Root Dir Input
            //if(currentModpack.PackPath != "")
            //{
                //FilesManager.Directories.Root = currentModpack.PackPath;
            //}
            Runner.Configuration config = new Runner.Configuration()
            {
                RamMax = currentModpack.RamMax,
                JvmArgs = currentModpack.JvmArgs,
                GameArgs = currentModpack.GameArgs,
                CustomWindowSize = currentModpack.CustomWindowSize,
                WindowSize = currentModpack.WindowSize,//new(320, 200),
                Version = currentModpack.Version.Id,//version,
                Type = Runner.Configuration.VersionType.OfficialMojang,
                ForceOffline = Config.ForceOffline,
                DemoUser = Config.DemoUser,
                Account = new Account()
                {
                    Id = Config.SelectedAccountId,
                    Type = Account.AuthType.None,
                    Name = User.Name
                }
            };
            /*
            Runner.Configuration config = new Runner.Configuration()
            {
                RamMax = Config.RamMax,
                JvmArgs = Config.JvmArgs,
                GameArgs = Config.GameArgs,
                CustomWindowSize = Config.CustomWindowSize,
                WindowSize = Config.WindowSize,//new(320, 200),
                Version = Config.Version.Id,//version,
                Type = Runner.Configuration.VersionType.OfficialMojang,
                ForceOffline = Config.ForceOffline,
                DemoUser = Config.DemoUser,
                Account = new Account()
                {
                    Id = Config.SelectedAccountId,
                    Type = Account.AuthType.None,
                    Name = User.Name
                }
            };
            */
            //string startStr = Runner.GenerateCommand(MojangFetcher.GetMain(Config.Version.Id), config);
            string startStr = Runner.GenerateCommand(MojangFetcher.GetMain(config.Version), config);
            AnsiConsole.WriteLine("[INF] Running The Game");

            ProcessStartInfo JavaProcessStartInfo = new ProcessStartInfo
            {
                Arguments = startStr,
                FileName = "java.exe",
                //RedirectStandardError = true,
                UseShellExecute = true
            };
            Process JavaProcess = new Process();
            JavaProcess.StartInfo = JavaProcessStartInfo;
            if (!JavaProcess.Start())
            {
                _progressPanel.IsVisible = false;
            };
            JavaProcess.WaitForExit();
            AnsiConsole.WriteLine("[INF] Stopping The Game");
        }
    }
    #endregion

    #region Helpers
    private ButtonResult ShowMessage(
        string message, 
        string title, 
        ButtonEnum button = ButtonEnum.Ok, 
        Icon icon = MessageBox.Avalonia.Enums.Icon.Error,
        string progressInfoText = ""
    )
    {
        new Thread(async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _progressBar.IsIndeterminate = false;
                _progressPanel.IsVisible = false;
                _progressInfo.Text = "";
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        Icon = icon,
                        ButtonDefinitions = button,
                        ContentMessage = message,
                        ContentTitle = title
                    });
                return msBoxStandardWindow.Show();
            });
        }).Start();
        return new ButtonResult();
    }
    #endregion
}