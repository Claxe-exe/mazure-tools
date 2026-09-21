using System.Windows;
using System.Windows.Controls;

namespace MazureTools.Views.Controls;

/// <summary>Titled tile with a big value and a load bar (used for CPU / RAM / GPU).</summary>
public partial class UsageCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = Register(nameof(Title), typeof(string), string.Empty);
    public static readonly DependencyProperty ValueTextProperty = Register(nameof(ValueText), typeof(string), "—");
    public static readonly DependencyProperty PercentProperty = Register(nameof(Percent), typeof(double), 0.0);
    public static readonly DependencyProperty DetailProperty = Register(nameof(Detail), typeof(string), string.Empty);
    public static readonly DependencyProperty NoteProperty = Register(nameof(Note), typeof(string), string.Empty);

    public UsageCard()
    {
        InitializeComponent();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string ValueText { get => (string)GetValue(ValueTextProperty); set => SetValue(ValueTextProperty, value); }

    public double Percent { get => (double)GetValue(PercentProperty); set => SetValue(PercentProperty, value); }

    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

    public string Note { get => (string)GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

    private static DependencyProperty Register(string name, Type type, object defaultValue) =>
        DependencyProperty.Register(name, type, typeof(UsageCard), new PropertyMetadata(defaultValue));
}
