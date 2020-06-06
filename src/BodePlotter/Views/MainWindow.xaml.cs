using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using BodePlotter.ViewModels;

namespace BodePlotter.Views
{
    internal sealed class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
