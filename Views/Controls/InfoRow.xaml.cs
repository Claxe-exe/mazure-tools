using System.Windows;
using System.Windows.Controls;

namespace MazureTools.Views.Controls;

/// <summary>Label / value pair used in the information cards.</summary>
public partial class InfoRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(InfoRow), new PropertyMetadata("—"));

    public InfoRow()
    {
        InitializeComponent();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}
