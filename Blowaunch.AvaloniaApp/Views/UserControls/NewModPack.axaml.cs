using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Blowaunch.AvaloniaApp.Views.UserControls;

public class NewPackControl : UserControl
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