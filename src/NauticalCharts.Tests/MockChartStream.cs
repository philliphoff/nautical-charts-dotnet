using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NauticalCharts.Tests
{
    internal static class MockChartStream
    {
        public static async Task<Stream> CreateAsync(
            string[] textSegment,
            byte bitDepth)
        {
            var stream = new MemoryStream();

            using var streamWriter = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

            foreach (var line in textSegment)
            {
                await streamWriter.WriteAsync(line);
            }

            await stream.WriteAsync(new byte[] { 0x1A, 0x00, bitDepth });
            await stream.FlushAsync();

            stream.Seek(0, SeekOrigin.Begin);   

            return stream;
        }
    }
}