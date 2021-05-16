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
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream, Encoding.ASCII);

            await streamWriter.WriteAsync("VER/3.07\r\n");

            stream.Seek(0, SeekOrigin.Begin);

            var chart = await BsbChartReader.ReadChartAsync(stream);

            stream.Dispose();

            Assert.NotNull(chart);
            Assert.NotNull(chart.TextSegment);
            Assert.Equal(1, chart.TextSegment.Count());
        }
    }
}
