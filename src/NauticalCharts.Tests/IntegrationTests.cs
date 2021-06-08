using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace NauticalCharts.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task ImageConversionTest()
        {
            using var stream = File.OpenRead("../../../../../assets/test/344102.KAP");

            var chart = await BsbChartReader.ReadChartAsync(stream);

            using var actualImage = chart.ToImage();

            using var expectedImage = await Image.LoadAsync<Rgba32>("../../../../../assets/test/344102.png");

            ImageAssert.Equal(expectedImage, actualImage);
        }
    }
}