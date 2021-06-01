using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbColor(byte R, byte G, byte B);

    public record BsbSize(int Height, int Width);

    public record BsbMetadata
    {
        public IReadOnlyDictionary<byte, BsbColor> Palette { get; init; }

        public BsbSize Size { get; init; }
    }
}