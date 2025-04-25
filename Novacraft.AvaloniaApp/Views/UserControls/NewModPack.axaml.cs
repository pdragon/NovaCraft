using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Novacraft.AvaloniaApp.Views.UserControls;

partial class NewPackControl : UserControl
{
    public NewPackControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}