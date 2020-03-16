using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using CSharpMath.Interfaces;
using CSharpMath.Rendering;
using CSharpMath.SkiaSharp;
using TextAlignment = CSharpMath.Rendering.TextAlignment;

namespace BodePlotter.Controls
{
    internal sealed class CSharpMathSkiaBlock : Control
    {
        public static readonly StyledProperty<float> FontSizeProperty =
          AvaloniaProperty.Register<CSharpMathSkiaBlock, float>(nameof(FontSize));

        public static readonly DirectProperty<CSharpMathSkiaBlock, MathSource> SourceProperty =
            AvaloniaProperty.RegisterDirect<CSharpMathSkiaBlock, MathSource>(
                nameof(Source),
                block => block.Source,
                (block, source) => block.Source = source);

        private readonly MathPainter _painter = new MathPainter();

        private MathSource _source;

        static CSharpMathSkiaBlock()
        {
            TypeDescriptor.AddAttributes(typeof(MathSource), new TypeConverterAttribute(typeof(MathSourceTypeConverter)));

            ClipToBoundsProperty.OverrideDefaultValue<CSharpMathSkiaBlock>(true);

            FontSizeProperty.Changed.AddClassHandler<CSharpMathSkiaBlock>(UpdateFontSize);
            SourceProperty.Changed.AddClassHandler<CSharpMathSkiaBlock>(UpdateSource);

            AffectsMeasure<CSharpMathSkiaBlock>(FontSizeProperty, SourceProperty);
        }

        public float FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        [TypeConverter(typeof(MathSourceTypeConverter))]
        public MathSource Source
        {
            get => _source;
            set => SetAndRaise(SourceProperty, ref _source, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var measure = _painter.Measure;

            if (measure.HasValue)
            {
                return new Size(measure.Value.Width, measure.Value.Height);
            }

            return default;
        }

        public override void Render(DrawingContext context)
        {
            context.Custom(new CSharpMathDrawOperation(_painter, Bounds));
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(InvalidateVisual, Avalonia.Threading.DispatcherPriority.Background);
        }

        private static void UpdateFontSize(CSharpMathSkiaBlock block, AvaloniaPropertyChangedEventArgs e) => block._painter.FontSize = (float)e.NewValue;

        private static void UpdateSource(CSharpMathSkiaBlock block, AvaloniaPropertyChangedEventArgs e) => block._painter.Source = (MathSource)e.NewValue;
    }

    internal sealed class MathSourceTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || sourceType == typeof(IMathList);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value switch
            {
                string str => new MathSource(str),
                IMathList list => new MathSource(list),
                _ => throw new NotSupportedException()
            };
    }

    internal sealed class CSharpMathDrawOperation : ICustomDrawOperation
    {
        private readonly MathPainter _painter;

        public CSharpMathDrawOperation(MathPainter painter, Rect bounds)
        {
            _painter = painter;

            Bounds = bounds;
        }

        public Rect Bounds { get; }

        public void Dispose() { }

        public bool Equals([AllowNull] ICustomDrawOperation other) => false;

        public bool HitTest(Point p) => false;

        public void Render(IDrawingContextImpl context)
        {
            var skiaContext = (ISkiaDrawingContextImpl)context;
            _painter.Draw(skiaContext.SkCanvas, TextAlignment.TopLeft);
        }
    }
}
