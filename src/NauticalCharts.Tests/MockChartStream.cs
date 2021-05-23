using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NauticalCharts.Tests
{
    internal static class MockChartStream
    {
        public static async Task<Stream> CreateAsync(string textSegment)
        {
            var stream = new MemoryStream();

            using var streamWriter = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

            await streamWriter.WriteAsync(textSegment);

            stream.Seek(0, SeekOrigin.Begin);   

            return stream;
        }
    }
}