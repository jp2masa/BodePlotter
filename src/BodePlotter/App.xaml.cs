using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Expr = MathNet.Symbolics.SymbolicExpression;

using BodePlotter.ViewModels;
using BodePlotter.Views;

namespace BodePlotter
{
    public class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var s = Expr.Variable("s");
                var vm = new MainWindowViewModel(s);

                vm.TransferFunctionInput = "s / (s + 2 * pi * 100)";
                vm.PlotCommand.Execute(null);

                desktop.MainWindow = new MainWindow() { DataContext = vm };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void Main(string[] args) =>
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        private static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                      .UseSkia()
                      .UsePlatformDetect()
                      .UseReactiveUI()
                      .LogToTrace();
    }
}
