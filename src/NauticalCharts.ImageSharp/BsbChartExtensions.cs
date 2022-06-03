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

            var image = new Image<Rgba32>((int)size.Width, (int)size.Height);

            try
            {
                Func<BsbColor, Rgba32> converter = color => new Rgba32(color.R, color.G, color.B, 0xFF);

                image.ProcessPixelRows(
                    accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            var rowSpan = accessor.GetRowSpan(y);

                            BsbChartWriter.WriteRasterRow(chart.RasterSegment, palette, y, rowSpan, converter);
                        }

                    });

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