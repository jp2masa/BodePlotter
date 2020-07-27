using System.Numerics;

namespace BodePlotter
{
    public static class ComplexExtensions
    {
        public static double MagnitudeSquared(this Complex complex) =>
            complex.Real * complex.Real + complex.Imaginary * complex.Imaginary;
    }
}
