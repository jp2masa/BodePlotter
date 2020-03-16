using System;
using System.Windows.Input;

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

        private string _function;
        private string _input;

        private int _fromFrequency = 0;
        private int _toFrequency = 10000;

        public MainWindowViewModel(Expr s)
        {
            _s = s;

            PlotCommand = ReactiveCommand.Create(Plot);

            MagnitudePlot = new PlotModel { Title = "Magnitude" };

            MagnitudePlot.Axes.Add(new LogarithmicAxis() { Position = AxisPosition.Bottom, Unit = "Hz" });
            MagnitudePlot.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Unit = "dB" });

            MagnitudePlot.Series.Add(new LineSeries());

            PhasePlot = new PlotModel { Title = "Phase" };

            PhasePlot.Axes.Add(new LogarithmicAxis() { Position = AxisPosition.Bottom, Unit = "Hz" });
            PhasePlot.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Unit = "º", Minimum = -180, Maximum = 180, MajorStep = 30 });

            PhasePlot.Series.Add(new LineSeries());
        }

        public string TransferFunction
        {
            get => _function;
            private set => this.RaiseAndSetIfChanged(ref _function, value);
        }

        public string TransferFunctionInput
        {
            get => _input;
            set => this.RaiseAndSetIfChanged(ref _input, value);
        }

        public int FromFrequency
        {
            get => _fromFrequency;
            set => this.RaiseAndSetIfChanged(ref _fromFrequency, value);
        }

        public int ToFrequency
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
                TransferFunction = "H(s) = " + H.ToLaTeX();

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

        private (LineSeries magnitude, LineSeries phase) PlotImpl(Expr H, int from, int to)
        {
            var h = H.CompileComplex(_s.VariableName);

            var magnitude = new LineSeries();
            var phase = new LineSeries();

            var n = 100;

            var df = to - from;
            var log = Math.Log10(df);

            for (int i = 0; i <= 100; i++)
            {
                var f = from + Math.Pow(10, i * log / n);
                var c = h(new System.Numerics.Complex(0, 2 * Math.PI * f));

                magnitude.Points.Add(new DataPoint(f, 10 * Math.Log10(c.Real * c.Real + c.Imaginary * c.Imaginary)));
                phase.Points.Add(new DataPoint(f, c.Phase * 180 / Math.PI));
            }

            return (magnitude, phase);
        }
    }
}
