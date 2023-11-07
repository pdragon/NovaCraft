using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using static Blowaunch.Library.LauncherConfig;

namespace Blowaunch.AvaloniaApp.Views.UserControls;

public class ModPackControl : UserControl
{
    private Image? _image;
    private TextBlock _modPackLabel = new();
    private Image _mainImage = new();

    private readonly ModPack ModPack = new();

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public ModPackControl()
    {
        InitializeComponent();
        InitializeFields();
        LoadItems();
    }

    //public ModPackControl(ModPack modPack, Action<string> OnclickCallback)
    public ModPackControl(ModPack modPack)
    {
        ModPack = modPack;
        InitializeComponent();
        InitializeFields();
        LoadItems();
    }

    private void InitializeFields()
    {
        _image = this.FindControl<Image>("ModPackImage");
        _modPackLabel = this.FindControl<TextBlock>("ModPackLabel");
        _mainImage = this.FindControl<Image>("MainImage");
    }

    private void LoadItems()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory; //Directory.GetCurrentDirectory();
        DirectoryInfo diPath = new DirectoryInfo(@path);
        string iconPath = "";
        string targetIconPath = Path.Combine(ModPack.PackPath, "server-icon.png");
        if (_image != null)
        {
            _modPackLabel.Text = ModPack.Name;
            //TODO: download from inet on first start, and load then from .blowaunch folder
            //iconPath = File.Exists(targetIconPath) ? targetIconPath : Path.Combine(path, "server-icon.png");
            //iconPath = File.Exists(targetIconPath)? targetIconPath: Path.Combine(path, "minecraft-small.png");
            if (File.Exists(targetIconPath))
            {
                iconPath = targetIconPath;
            }
            else
            {
                _image.Tag = "If you what see here your own image here, please copy you png file into: " + targetIconPath;
                iconPath = Path.Combine(path, "minecraft-small.png");
            }
            using var fileStream = File.OpenRead(iconPath);
            _image.Source = new Bitmap(fileStream);
            this.Name = ModPack.Id;
        }
        if(_mainImage != null)
        {
            //TODO: download from inet on first start, and load then from .blowaunch folder
            iconPath = File.Exists(targetIconPath) ? targetIconPath : Path.Combine(path, "minecraft.png");
            using var fileStream = File.OpenRead(iconPath);
            _mainImage.Source = new Bitmap(fileStream);
        }
    }
    
    private void OnEraseModPack(object? sender, RoutedEventArgs args)
    {
        //(sender as Button)!.Content = "Test";
    }
    
}