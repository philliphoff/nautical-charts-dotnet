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
            Assert.True(chart.BitDepth.HasValue);
            Assert.Equal(1, chart.BitDepth.Value);
        }

        [Fact]
        public async Task ReadSampleChart()
        {
            using var stream = File.OpenRead("../../../../../assets/test/344102.KAP");

            var chart = await BsbChartReader.ReadChartAsync(stream);

            Assert.NotNull(chart);
            Assert.NotNull(chart.TextSegment);
            Assert.NotNull(chart.RasterSegment);
            Assert.Equal(2098, chart.RasterSegment.Count());

            uint index = 1;

            foreach (var row in chart.RasterSegment)
            {
                Assert.Equal(index++, row.RowNumber);

                Assert.Equal<uint>(1171, row.Runs.Aggregate<BsbRasterRun, uint>(0, (sum, run) => sum + run.Length));
            }
        }
    }
}
