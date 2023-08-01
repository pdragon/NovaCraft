using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using static Blowaunch.AvaloniaApp.LauncherConfig;

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
        if (_image != null)
        {
            _modPackLabel.Text = ModPack.Name;
            using var fileStream = File.OpenRead("f:/tmp/3/minecraft-small.png");
            _image.Source = new Bitmap(fileStream);
            this.Name = ModPack.Id;
        }
        if(_mainImage != null)
        {
            using var fileStream = File.OpenRead("f:/tmp/3/minecraft.png");
            _mainImage.Source = new Bitmap(fileStream);
        }
    }
    
    private void OnEraseModPack(object? sender, RoutedEventArgs args)
    {
        (sender as Button)!.Content = "Test";
    }
    
}