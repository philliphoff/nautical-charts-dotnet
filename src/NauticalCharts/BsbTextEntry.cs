using System.Collections.Generic;

namespace NauticalCharts
{
    public record BsbTextEntry (string EntryType, IEnumerable<string> Lines);
}
