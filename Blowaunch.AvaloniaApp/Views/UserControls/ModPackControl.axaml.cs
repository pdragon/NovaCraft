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
    private TextBlock _modPackLabel;
    private MainWindow MainWindow;

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
    public ModPackControl(ModPack modPack, MainWindow mainWindow)
    {
        ModPack = modPack;
        InitializeComponent();
        InitializeFields();
        LoadItems();
        MainWindow = mainWindow;
    }

    private void InitializeFields()
    {
        _image = this.FindControl<Image>("modPackImage");
        _modPackLabel = this.FindControl<TextBlock>("ModPackLabel");
    }

    private void LoadItems()
    {
        if (_image != null)
        {
            _modPackLabel.Text = ModPack.Name;
            using var fileStream = File.OpenRead("f:/tmp/3/minecraft.png");
            _image.Source = new Bitmap(fileStream);
            this.Name = ModPack.Id;
        }
    }
    
    private void OnEraseModPack(object? sender, RoutedEventArgs args)
    {
        (sender as Button)!.Content = "Test";
        MainWindow.OnEraseModPack(ModPack);
    }
    
}