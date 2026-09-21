using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace MazureTools.Modules.Network;

public partial class NetworkView : UserControl
{
    public NetworkView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // Pure view behaviour: keep the newest ping line visible.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NetworkViewModel oldVm)
            oldVm.Ping.Lines.CollectionChanged -= OnLinesChanged;
        if (e.NewValue is NetworkViewModel newVm)
            newVm.Ping.Lines.CollectionChanged += OnLinesChanged;
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => PingScroll.ScrollToEnd();
}
