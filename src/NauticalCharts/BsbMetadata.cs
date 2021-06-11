using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbColor(byte R, byte G, byte B);

    public record BsbCoordinate(double Latitude, double Longitude);

    public record BsbSize(int Height, int Width);

    public record BsbMetadata
    {
        public IReadOnlyList<BsbCoordinate> Border { get; init; }

        public IReadOnlyDictionary<byte, BsbColor> Palette { get; init; }

        public BsbSize Size { get; init; }
    }
}