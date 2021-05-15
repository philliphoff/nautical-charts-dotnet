using System.Threading.Tasks;
using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbChartReaderTests
    {
        [Fact]
        public async Task ReturnsNullChartAsync()
        {
            var chart = await BsbChartReader.ReadChartAsync(null);

            Assert.Null(chart);
        }
    }
}
