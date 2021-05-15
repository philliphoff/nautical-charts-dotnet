using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbChart (IEnumerable<BsbTextEntry> TextSegment, IEnumerable<BsbRasterRow> RasterSegment);
}
