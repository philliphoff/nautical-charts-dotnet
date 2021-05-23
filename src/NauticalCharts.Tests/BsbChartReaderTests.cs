using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbChartReaderTests
    {
        [Fact]
        public async Task ReadsTextSegment()
        {
            using var stream = await MockChartStream.CreateAsync("VER/3.07\r\n", 1);

            var chart = await BsbChartReader.ReadChartAsync(stream);

            Assert.NotNull(chart);
            Assert.NotNull(chart.TextSegment);
            Assert.Equal(1, chart.TextSegment.Count());
            Assert.NotNull(chart.TextSegment.First().Lines);
            Assert.Equal(1, chart.TextSegment.First().Lines.Count());
            Assert.Equal("VER/3.07", chart.TextSegment.First().Lines.First());
        }
    }
}
