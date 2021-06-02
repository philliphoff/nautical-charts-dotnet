using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbChart (IEnumerable<BsbTextEntry> TextSegment, byte? BitDepth, IReadOnlyDictionary<uint, IEnumerable<BsbRasterRun>> RasterSegment);
}
