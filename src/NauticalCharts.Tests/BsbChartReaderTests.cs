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
            await stream.FlushAsync();
//            await streamWriter.FlushAsync();

            stream.Seek(0, SeekOrigin.Begin);

            var chart = await BsbChartReader.ReadChartAsync(stream);

            stream.Dispose();

            Assert.NotNull(chart);
            Assert.NotNull(chart.TextSegment);
            Assert.Equal(1, chart.TextSegment.Count());
//            Assert.NotNull(chart.TextSegment.First().Lines);
//            Assert.Equal(1, chart.TextSegment.First().Lines.Count());
//            Assert.Equal("VER/3.07", chart.TextSegment.First().Lines.First());
        }
    }
}
