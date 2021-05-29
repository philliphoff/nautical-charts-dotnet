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
            using var stream = await MockChartStream.CreateAsync(
                new[] {
                    "KNP/SC=40000,GD=WGS84,PR=Mercator,PP=48.666667,PI=4.000,SK=0.000000\r\n",
                    "    TA=90.000000,UN=METRES,SD=Lower Low Water Large Tide,DX=4.000000\r\n",
                    "    DY=4.000000\r\n",
                    "KNQ/EC=WE,GD=WGE,VC=HHLT,SC=LLLT,PC=MC,P1=0.000000,P2=48.666667\r\n",
                    "    P3=NOT_APPLICABLE,P4=NOT_APPLICABLE,GC=UB,RM=INVERSE\r\n"
                },
                1);

            var chart = await BsbChartReader.ReadChartAsync(stream);

            Assert.NotNull(chart);
            Assert.NotNull(chart.TextSegment);

            var textSegment = chart.TextSegment.ToArray();

            Assert.Equal(2, textSegment.Length);

            Assert.Equal("KNP", textSegment[0].EntryType);
            Assert.Equal("KNQ", textSegment[1].EntryType);

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
