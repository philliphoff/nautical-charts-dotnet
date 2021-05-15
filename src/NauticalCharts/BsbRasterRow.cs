using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbRasterRow (int RowNumber, IEnumerable<BsbRasterRun> Runs);
}
