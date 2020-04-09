using System;
using System.Windows.Input;

using CSharpMath.Atom;

using Expr = MathNet.Symbolics.SymbolicExpression;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using ReactiveUI;

namespace BodePlotter.ViewModels
{
    internal sealed class MainWindowViewModel : ReactiveObject
    {
        private readonly Expr _s;

        private MathList _function;
        private string _input;

        private double _fromFrequency = 0.01;
        private double _toFrequency = 1000000;

        public MainWindowViewModel(Expr s)
        {
            _s = s;

            PlotCommand = ReactiveCommand.Create(Plot);

            MagnitudePlot = new PlotModel { Title = "Magnitude" };

            MagnitudePlot.Axes.Add(CreateFrequencyAxis(AxisPosition.Bottom));
            MagnitudePlot.Axes.Add(InitializeAxis(new LinearAxis() { Position = AxisPosition.Left, Title = "|H(s)|", Unit = "dB", AxisTitleDistance = 32 }));

            MagnitudePlot.Series.Add(new LineSeries());

            PhasePlot = new PlotModel { Title = "Phase" };

            PhasePlot.Axes.Add(CreateFrequencyAxis(AxisPosition.Bottom));
            PhasePlot.Axes.Add(InitializeAxis(new LinearAxis() { Position = AxisPosition.Left, Title = "/_ H(s)", Unit = "°", Minimum = -180, Maximum = 180, MajorStep = 45, AxisTitleDistance = 32 }));

            PhasePlot.Series.Add(new LineSeries());
        }

        public MathList TransferFunction
        {
            get => _function;
            private set => this.RaiseAndSetIfChanged(ref _function, value);
        }

        public string TransferFunctionInput
        {
            get => _input;
            set => this.RaiseAndSetIfChanged(ref _input, value);
        }

        public double FromFrequency
        {
            get => _fromFrequency;
            set => this.RaiseAndSetIfChanged(ref _fromFrequency, value);
        }

        public double ToFrequency
        {
            get => _toFrequency;
            set => this.RaiseAndSetIfChanged(ref _toFrequency, value);
        }

        public ICommand PlotCommand { get; }

        public PlotModel MagnitudePlot { get; }

        public PlotModel PhasePlot { get; }

        private void Plot()
        {
            try
            {
                var H = Expr.Parse(TransferFunctionInput);
                TransferFunction = LaTeXParser.MathListFromLaTeX("H(s) = " + H.ToLaTeX());

                var (magnitude, phase) = PlotImpl(H, FromFrequency, ToFrequency);

                MagnitudePlot.Series[0] = magnitude;
                PhasePlot.Series[0] = phase;

                MagnitudePlot.InvalidatePlot(true);
                PhasePlot.InvalidatePlot(true);
            }
            catch
            {
            }
        }

        private (LineSeries magnitude, LineSeries phase) PlotImpl(Expr H, double from, double to)
        {
            var h = H.CompileComplex(_s.VariableName);

            var magnitude = new LineSeries();
            var phase = new LineSeries();

            var n = 100;

            var df = to / from;
            var log = Math.Log10(df);

            for (int i = 0; i <= n; i++)
            {
                var f = from * Math.Pow(10, i * log / n);
                var c = h(new System.Numerics.Complex(0, 2 * Math.PI * f));

                magnitude.Points.Add(new DataPoint(f, 10 * Math.Log10(c.MagnitudeSquared())));
                phase.Points.Add(new DataPoint(f, c.Phase * 180 / Math.PI));
            }

            return (magnitude, phase);
        }

        private static LogarithmicAxis CreateFrequencyAxis(AxisPosition position) =>
            InitializeAxis(
                new LogarithmicAxis()
                {
                    Position = position,
                    Title = "f",
                    Unit = "Hz",
                    UseSuperExponentialFormat = true
                }
            );

        private static T InitializeAxis<T>(T axis)
            where T : Axis
        {
            axis.FontSize = 14;
            axis.TitlePosition = 0.95;
            axis.TitleFormatString = "{0} ({1})";

            return axis;
        }
    }
}
