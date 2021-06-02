using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbChartWriterTests
    {
        [Fact]
        public async Task IntegrationTest()
        {
            using var stream = File.OpenRead("../../../../../assets/test/344102.KAP");

            var chart = await BsbChartReader.ReadChartAsync(stream);

            var metadata = BsbMetadataReader.ReadMetadata(chart.TextSegment);

            using var actualImage = new Image<Rgba32>(metadata.Size.Width, metadata.Size.Height);

            actualImage.Mutate(c => c.ProcessPixelRowsAsVector4(
                (row, point) =>
                {
                    Debug.Assert(point.X == 0, "Cannot assume entire rows.");

                    BsbChartWriter.WriteRasterRow(chart.RasterSegment, metadata.Palette, (uint)point.Y, row);
                }));

            using var expectedImage = await Image.LoadAsync<Rgba32>("../../../../../assets/test/344102.png");

            ImageAssert.Equal(expectedImage, actualImage);
        }
    }
}