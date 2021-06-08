using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace NauticalCharts
{
    public static class BsbChartExtensions
    {
        public static Image<Rgba32> ToImage(this BsbChart chart)
        {
            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            var metadata = BsbMetadataReader.ReadMetadata(chart.TextSegment);

            return ToImage(chart, metadata.Palette, metadata.Size);
        }

        public static Image<Rgba32> ToImage(this BsbChart chart, IReadOnlyDictionary<byte, BsbColor> palette, BsbSize size)
        {
            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            var image = new Image<Rgba32>(size.Width, size.Height);

            try
            {
                Func<BsbColor, Rgba32> converter = color => new Rgba32(color.R, color.G, color.B, 0xFF);

                Action<int> rowSetter =
                    y =>
                    {
                        var rowSpan = image.GetPixelRowSpan(y);

                        BsbChartWriter.WriteRasterRow(chart.RasterSegment, palette, y, rowSpan, converter);
                    };

                for (int y = 0; y < image.Height; y++)
                {
                    rowSetter(y);
                }

                return image;
            }
            catch
            {
                image.Dispose();

                throw;
            }
        }
    }
}