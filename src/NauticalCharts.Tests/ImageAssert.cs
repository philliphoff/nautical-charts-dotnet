using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace NauticalCharts.Tests
{
    internal static class ImageAssert
    {
        public static void Equal(Image<Rgba32> expected, Image<Rgba32> actual)
        {
            Assert.Equal(expected.Height, actual.Height);
            Assert.Equal(expected.Width, actual.Width);

            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    Assert.Equal(expected[x, y], actual[x, y]);
                }
            }
        }
    }
}