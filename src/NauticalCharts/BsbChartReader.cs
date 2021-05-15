using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NauticalCharts
{
    public static class BsbChartReader
    {
        public static Task<BsbChart> ReadChartAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BsbChart>(null);
        }
    }
}
