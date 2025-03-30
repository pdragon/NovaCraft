using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blowaunch.Library.UsableClasses.ShareModPack;
using DynamicData;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;

//using static Blowaunch.Library.LauncherConfig;

using static Blowaunch.Library.UsableClasses.ShareModPack.ExportFileParams;

namespace Blowaunch.AvaloniaApp.Views.UserControls;

public partial class UploadModpack : UserControl
{
    List<ShareAccount> ShareAccountConfig = new();
    //private ComboBox _accountsCombo;
    private TextBox? _shareFTPUsername;
    private TextBox? _shareFTPPassword;
    private TextBox? _shareFTPServer;
    public Border? _shareBorder;
    private ComboBox? _accountsCombo;
    private TextBox? _profileName;
    private TextBlock? _errorMessage;
    private TextBox? _shareFTPRootPath;
    private TextBlock? _guidText;
    private ComboBox? _shareType;

    private string? CurrentGuid;

    public UploadModpack()
    {
        InitializeComponent();
        InitializeFields();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeFields()
    {

        _profileName      = this.FindControl<TextBox>("ProfileName");
        _shareFTPUsername = this.FindControl<TextBox>("ShareFTPUsername");
        _shareFTPPassword = this.FindControl<TextBox>("ShareFTPPassword");
        _shareFTPServer   = this.FindControl<TextBox>("ShareFTPServer");
        _shareBorder      = this.FindControl<Border>("ShareBorder");
        _accountsCombo    = this.FindControl<ComboBox>("AccountsCombo");
        _errorMessage     = this.FindControl<TextBlock>("ErrorMessage");
        _shareFTPRootPath = this.FindControl<TextBox>("ShareFTPRootPath");
        _guidText         = this.FindControl<TextBlock>("GuidText");
        _shareType        = this.FindControl<ComboBox>("ShareType");
        

        LoadConfig();
        if (_shareType != null)
        {
            _shareType.ItemsSource = ExportFileParams.ShareTypeList;
        }
        //_accountsCombo.Items = ShareAccountConfig;//.Select(a => a.Name).ToList();
        if (_accountsCombo != null)
        {
            _accountsCombo.ItemsSource = ShareAccountConfig;
            _accountsCombo.SelectedIndex = 0;
            _accountsCombo.SelectionChanged += AccountsComboChanged;
        }
    }

    public int GetShareAccountConfig()
    {
        LoadConfig();
        return ShareAccountConfig.Count();
    }

    public void AccountsComboChanged(object? sender, RoutedEventArgs e)
    {
        if (_accountsCombo != null && (ShareAccount?)_accountsCombo!.SelectedItem != null)
        {
            CurrentGuid = ((ShareAccount?)_accountsCombo!.SelectedItem!).Guid;
            if (_guidText != null)
            {
                _guidText.Text = CurrentGuid;
            }
        }
        //LoadConfig();
        //var item = _accountsCombo!.SelectedItem;
        //try
        //{
        //    _accountsCombo!.ItemsSource = (List<ShareAccount>)ShareAccountConfig;
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine(ex.Message.ToString());
        //}
        //_accountsCombo!.SelectedItem = item;

        SetCombos();
    }


    public int ShareOpen()
    {
        _shareBorder!.IsVisible = true;
        return ShareAccountConfig.Count();
    }

    public void ShareClose(object sender, RoutedEventArgs e)
    => _shareBorder!.IsVisible = false;

    public void ShareModpackAdd(object sender, RoutedEventArgs e)
    {
        LoadConfig();
        ClearFields();
        CurrentGuid = null;
        if (_guidText != null)
        {
            _guidText.Text = null;
        }
    }

    public void FtpSave(object sender, RoutedEventArgs e)
    {
        //if (!string.IsNullOrEmpty(ShareAccountConfig.Select(n => n.Name).Where(n => n == _profileName!.Text).FirstOrDefault()))
        //{
        //    _errorMessage!.IsVisible = true;
        //    return;
        //}
        ShareAccount? shareAccount = new ShareAccount();
        var currentInstance = new ShareAccount()
        {
            Login = _shareFTPUsername!.Text,
            Password = _shareFTPPassword!.Text,
            Server = _shareFTPServer!.Text,
            Name = _profileName!.Text,
            UploadDir = _shareFTPRootPath!.Text
        };
        var instanceExist = ShareAccountConfig.Where(s => s.Guid == CurrentGuid).FirstOrDefault();
        if (instanceExist == null)
        {
            shareAccount = currentInstance;
            // Insert
            if (CurrentGuid == null)
            {
                currentInstance.Guid = Guid.NewGuid().ToString();
                ShareAccountConfig.Add(currentInstance);
            }
        }
        else
        {
            //Update
            //_accountsCombo!.SelectedItem = instanceExist;
            foreach (var instance in ShareAccountConfig.Where(w => w.Guid == CurrentGuid))
            {

                instance.Login = _shareFTPUsername!.Text;
                instance.Password = _shareFTPPassword!.Text;
                instance.Server = _shareFTPServer!.Text;
                instance.UploadDir = _shareFTPRootPath!.Text;
                instance.Name = _profileName!.Text;
                if (_shareType != null && _shareType.SelectedItem != null)
                {
                    instance.UploadThrough = ((NamedPair)_shareType.SelectedItem).Value;
                }
                //instance.Guid = CurrentGuid;
                shareAccount = instance;
                shareAccount.Guid = instance.Guid;
            }
        }
        SaveConfig();
        LoadConfig();
        
        if (((List<ShareAccount>)_accountsCombo!.ItemsSource!).Count() == 0)
        {
            ((List<ShareAccount>)_accountsCombo!.ItemsSource!).Add(new ShareAccount());
        }
        _accountsCombo!.ItemsSource = ShareAccountConfig;
        _accountsCombo!.SelectedIndex = ShareAccountConfig.Count() - 1;
        _errorMessage!.IsVisible = false;
        //ClearFields();
    }


    public void EraseInstance(object sender, RoutedEventArgs e)
    {
        if (_accountsCombo!.SelectedItem != null)
        {
            var instance = (Blowaunch.Library.UsableClasses.ShareModPack.ExportFileParams.ShareAccount)_accountsCombo.SelectedItem;
            var findInstance = ShareAccountConfig.Find(i => i.Name == instance.Name);
            _accountsCombo.SelectedItem = ShareAccountConfig.FirstOrDefault();
            ShareAccountConfig.Remove(instance);
            CurrentGuid = null;
            var its = (List<ShareAccount>)_accountsCombo.ItemsSource!;
            if (its != null && its.Count > 0) {
                ((List<ShareAccount>)_accountsCombo.ItemsSource!).Remove(instance);
            }

            if (ShareAccountConfig != null && ShareAccountConfig.Count > 0)
            {
                if (_accountsCombo.Items != null)
                {
                    _accountsCombo.ItemsSource = new List<ShareAccount>();
                    SaveConfig();
                    _accountsCombo.ItemsSource = ShareAccountConfig;
                    _accountsCombo.SelectedItem = ShareAccountConfig.FirstOrDefault();
                }
            }
            ClearFields();
        }
        SaveConfig();
        LoadConfig();
    }


    private void LoadConfig()
    {
        string filePath = Path.Combine(Library.FilesManager.Directories.Root, $"share.json");
        var config = JsonConvert.DeserializeObject<List<ShareAccount>>(File.ReadAllText(filePath));
        if (config != null)
        {
            ShareAccountConfig = config;
        }
    }

    private void SaveConfig()
    {
        string filePath = Path.Combine(Library.FilesManager.Directories.Root, $"share.json");
        File.WriteAllText(filePath, JsonConvert.SerializeObject(ShareAccountConfig));
    }

    private void ClearFields()
    {
        _shareFTPUsername!.Text  = "";
        _shareFTPPassword!.Text  = "";
        _shareFTPServer!.Text    = "";
        _profileName!.Text       = "";
    }

    private void SetCombos()
    {
        if (_accountsCombo != null && _accountsCombo.SelectedItem != null)
        {
            var instance = (ShareAccount)_accountsCombo.SelectedItem;
            if (ShareAccountConfig != null)
            {
                ShareAccount? findedInstance = ShareAccountConfig.Find(i => i.Name == instance.Name);
                if (findedInstance != null)
                {
                    _shareFTPUsername!.Text = findedInstance.Login;
                    _shareFTPPassword!.Text = findedInstance.Password;
                    _shareFTPServer!.Text = findedInstance.Server;
                    _profileName!.Text = findedInstance.Name;
                    _shareFTPRootPath!.Text = findedInstance.UploadDir;
                    
                    CurrentGuid = findedInstance.Guid;

                }
            }
        }
    }
}