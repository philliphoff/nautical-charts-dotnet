using System.Collections.Generic;

namespace NauticalCharts
{
    public sealed record BsbColor(byte R, byte G, byte B);

    public sealed record BsbCoordinate(double Latitude, double Longitude);

    public sealed record BsbSize(int Height, int Width);

    public sealed record BsbMetadata
    {
        public IReadOnlyList<BsbCoordinate>? Border { get; init; }

        public string? Name { get; init; }

        public IReadOnlyDictionary<byte, BsbColor>? Palette { get; init; }

        public BsbSize? Size { get; init; }
    }
}