using System;
using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbRasterRow (uint RowNumber, IEnumerable<BsbRasterRun> Runs);
}
