using System.Windows;
using System.Windows.Controls;

namespace MazureTools.Views;

public enum MessageKind
{
    Info,
    Question,
    Warning,
    Danger,
    Error
}

/// <summary>Theme-aware replacement for MessageBox (the native one ignores dark mode).</summary>
public partial class MessageDialog : Window
{
    public MessageDialog(string title, string message, MessageKind kind, string confirmText, string? cancelText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        (IconText.Text, var brushKey) = kind switch
        {
            MessageKind.Error => ("", "DangerBrush"),
            MessageKind.Danger => ("", "DangerBrush"),
            MessageKind.Warning => ("", "WarningBrush"),
            MessageKind.Question => ("", "AccentBrush"),
            _ => ("", "AccentBrush")
        };
        IconText.SetResourceReference(TextBlock.ForegroundProperty, brushKey);

        ConfirmButton.Style = (Style)FindResource(kind == MessageKind.Danger ? "DangerButton" : "AccentButton");
        if (cancelText is null)
        {
            CancelButton.Visibility = Visibility.Collapsed;
            ConfirmButton.IsDefault = true;
        }
        else
        {
            CancelButton.Content = cancelText;
            // For dangerous actions Enter must mean "cancel", so a stray keypress cannot confirm.
            (kind == MessageKind.Danger ? CancelButton : ConfirmButton).IsDefault = true;
        }

        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
