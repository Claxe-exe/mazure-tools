using System.Windows;
using MazureTools.Core;
using WinForms = System.Windows.Forms;

namespace MazureTools.Services;

/// <summary>System tray icon (built-in NotifyIcon) with Open / Dashboard / Network / Exit.</summary>
public sealed class TrayService : IDisposable
{
    private readonly IWindowController _window;
    private readonly ISettingsService _settings;
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ContextMenuStrip _menu;
    private readonly WinForms.ToolStripMenuItem _open, _dashboard, _network, _exit;

    public TrayService(IWindowController window, ISettingsService settings)
    {
        _window = window;
        _settings = settings;

        _open = new WinForms.ToolStripMenuItem { Font = new System.Drawing.Font(WinForms.Control.DefaultFont, System.Drawing.FontStyle.Bold) };
        _open.Click += (_, _) => _window.Show();

        _dashboard = new WinForms.ToolStripMenuItem();
        _dashboard.Click += (_, _) => _window.Show(PageId.Dashboard);

        _network = new WinForms.ToolStripMenuItem();
        _network.Click += (_, _) => _window.Show(PageId.Network);

        _exit = new WinForms.ToolStripMenuItem();
        _exit.Click += (_, _) => _window.Exit();

        _menu = new WinForms.ContextMenuStrip();
        _menu.Items.AddRange([_open, _dashboard, _network, new WinForms.ToolStripSeparator(), _exit]);
        UpdateTexts();
        Loc.LanguageChanged += OnLanguageChanged;

        _icon = new WinForms.NotifyIcon
        {
            Text = "Mazure Tools",
            Icon = LoadIcon(),
            ContextMenuStrip = _menu,
            Visible = true
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                _window.Show();
        };

        _window.HiddenToTray += OnHiddenToTray;
    }

    private void OnHiddenToTray(object? sender, EventArgs e)
    {
        if (_settings.Current.TrayHintShown)
            return;

        _settings.Current.TrayHintShown = true;
        _settings.Save();
        _icon.ShowBalloonTip(4000, Loc.T("Tray_HintTitle"), Loc.T("Tray_HintText"), WinForms.ToolTipIcon.Info);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateTexts();

    private void UpdateTexts()
    {
        _open.Text = Loc.T("Tray_Open");
        _dashboard.Text = Loc.T("Nav_Dashboard");
        _network.Text = Loc.T("Nav_Network");
        _exit.Text = Loc.T("Tray_Exit");
    }

    private static System.Drawing.Icon LoadIcon()
    {
        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/mazure.ico", UriKind.Absolute))?.Stream;
        return stream is null
            ? System.Drawing.SystemIcons.Application
            : new System.Drawing.Icon(stream, WinForms.SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _window.HiddenToTray -= OnHiddenToTray;
        Loc.LanguageChanged -= OnLanguageChanged;
        _icon.Visible = false; // removes the icon immediately instead of leaving a ghost until mouse-over
        _icon.Dispose();
        _menu.Dispose();
    }
}
