using System.Windows;
using System.Windows.Controls;

namespace MazureTools.Views.Controls;

/// <summary>Small label + value tile for statistics.</summary>
public partial class StatTile : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatTile), new PropertyMetadata("—"));

    public StatTile()
    {
        InitializeComponent();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}
