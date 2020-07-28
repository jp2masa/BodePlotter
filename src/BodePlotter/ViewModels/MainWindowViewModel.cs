using System;
using System.Numerics;
using System.Windows.Input;

using CSharpMath.Atom;

using MathNet.Numerics;
using Expr = MathNet.Symbolics.SymbolicExpression;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using ReactiveUI;

namespace BodePlotter.ViewModels
{
    internal sealed class MainWindowViewModel : ReactiveObject
    {
        private const string MagnitudeAxisKey = "magnitude";
        private const string PhaseAxisKey = "phase";

        private readonly Expr _s;

        private readonly LogarithmicAxis _magnitudeFrequencyAxis;
        private readonly LogarithmicAxis _phaseFrequencyAxis;

        private readonly LogarithmicAxis _frequencyAxis;

        private MathList _function;
        private string _input;

        private double _fromFrequency = 0.01;
        private double _toFrequency = 1000000;

        private bool _isHz = true;
        private bool _isRadS;

        private bool _isStacked;
        private bool _isEffectivelyStacked;

        public MainWindowViewModel(Expr s)
        {
            _s = s;

            PlotCommand = ReactiveCommand.Create(Plot);
            ResetViewCommand = ReactiveCommand.Create(ResetView);

            MagnitudePlot = new PlotModel { Title = "Magnitude" };

            _magnitudeFrequencyAxis = CreateFrequencyAxis(AxisPosition.Bottom);

            MagnitudePlot.Axes.Add(_magnitudeFrequencyAxis);
            MagnitudePlot.Axes.Add(InitializeAxis(new LinearAxis() { Position = AxisPosition.Left, Title = "|H(s)|", Unit = "dB", AxisTitleDistance = 32 }));

            PhasePlot = new PlotModel { Title = "Phase" };

            _phaseFrequencyAxis = CreateFrequencyAxis(AxisPosition.Bottom);

            PhasePlot.Axes.Add(_phaseFrequencyAxis);
            PhasePlot.Axes.Add(InitializeAxis(new LinearAxis() { Position = AxisPosition.Left, Title = "∡H(s)", Unit = "°", Minimum = -180, Maximum = 180, MajorStep = 45, AxisTitleDistance = 32 }));

            BodePlot = new PlotModel { Title = "Bode Plot" };

            _frequencyAxis = CreateFrequencyAxis(AxisPosition.Bottom);

            var magnitudeAxis = InitializeAxis(new LinearAxis() { Key = MagnitudeAxisKey, Title = "|H(s)|", Unit = "dB", Position = AxisPosition.Left, StartPosition = 0.5, EndPosition = 1 });
            var phaseAxis = InitializeAxis(new LinearAxis() { Key = PhaseAxisKey, Title = "∡H(s)", Unit = "°", Minimum = -180, Maximum = 180, MajorStep = 45, Position = AxisPosition.Left, StartPosition = 0, EndPosition = 0.5 });

            BodePlot.Axes.Add(_frequencyAxis);
            BodePlot.Axes.Add(magnitudeAxis);
            BodePlot.Axes.Add(phaseAxis);
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

        public bool IsHz
        {
            get => _isHz;
            set => this.RaiseAndSetIfChanged(ref _isHz, value);
        }

        public bool IsRadS
        {
            get => _isRadS;
            set => this.RaiseAndSetIfChanged(ref _isRadS, value);
        }

        public bool IsStacked
        {
            get => _isStacked;
            set => this.RaiseAndSetIfChanged(ref _isStacked, value);
        }

        public bool IsEffectivelyStacked
        {
            get => _isEffectivelyStacked;
            set => this.RaiseAndSetIfChanged(ref _isEffectivelyStacked, value);
        }

        public ICommand PlotCommand { get; }

        public ICommand ResetViewCommand { get; }

        public PlotModel MagnitudePlot { get; }

        public PlotModel PhasePlot { get; }

        public PlotModel BodePlot { get; }

        private void Plot()
        {
            try
            {
                var H = Expr.Parse(TransferFunctionInput);
                TransferFunction = LaTeXParser.MathListFromLaTeX("H(s) = " + H.ToLaTeX()).Match(m => m, e => throw new Exception(e));

                MagnitudePlot.Series.Clear();
                PhasePlot.Series.Clear();
                BodePlot.Series.Clear();

                IsEffectivelyStacked = IsStacked;

                var (magnitude, phase) = PlotImpl(H, FromFrequency, ToFrequency);

                if (IsStacked)
                {
                    _frequencyAxis.Title = IsRadS ? "ω" : "f";
                    _frequencyAxis.Unit = IsRadS ? "rad/s" : "Hz";

                    magnitude.YAxisKey = MagnitudeAxisKey;
                    phase.YAxisKey = PhaseAxisKey;

                    BodePlot.Series.Add(magnitude);
                    BodePlot.Series.Add(phase);

                    BodePlot.InvalidatePlot(true);
                }
                else
                {
                    _phaseFrequencyAxis.Title = _magnitudeFrequencyAxis.Title = IsRadS ? "ω" : "f";
                    _phaseFrequencyAxis.Unit = _magnitudeFrequencyAxis.Unit = IsRadS ? "rad/s" : "Hz";

                    MagnitudePlot.Series.Add(magnitude);
                    PhasePlot.Series.Add(phase);

                    MagnitudePlot.InvalidatePlot(true);
                    PhasePlot.InvalidatePlot(true);
                }

                ResetView();
            }
            catch
            {
            }
        }

        private (LineSeries magnitude, LineSeries phase) PlotImpl(Expr H, double from, double to)
        {
            var h = H.CompileComplex(_s.VariableName);

            var magnitude = new LineSeries() { Title = "|H(s)|" };
            var phase = new LineSeries() { Title = "∡H(s)" };

            var n = 100;

            var df = to / from;
            var log = Math.Log10(df);

            for (int i = 0; i <= n; i++)
            {
                var f = from * Math.Pow(10, i * log / n);
                var c = h(new Complex(0, IsRadS ? f : (2 * Math.PI * f)));

                magnitude.Points.Add(new DataPoint(f, 10 * Math.Log10(c.MagnitudeSquared())));
                phase.Points.Add(new DataPoint(f, c.Phase * 180 / Math.PI));
            }

            return (magnitude, phase);
        }

        private void ResetView()
        {
            if (IsEffectivelyStacked)
            {
                BodePlot.ResetAllAxes();
                BodePlot.InvalidatePlot(false);
            }
            else
            {
                MagnitudePlot.ResetAllAxes();
                MagnitudePlot.InvalidatePlot(false);
                PhasePlot.ResetAllAxes();
                PhasePlot.InvalidatePlot(false);
            }
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
            axis.TitleFormatString = "{0} ({1})";

            return axis;
        }
    }
}
