using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NauticalCharts
{
    public static class BsbMetadataReader
    {
        public static BsbMetadata ReadMetadata(IEnumerable<BsbTextEntry> textEntries)
        {
            if (textEntries == null)
            {
                throw new ArgumentNullException(nameof(textEntries));
            }

            var metadata = new BsbMetadata();

            foreach (var textEntry in textEntries)
            {
                metadata = textEntry.EntryType switch
                {
                    "BSB" => ParseSize(metadata, textEntry),
                    "RGB" => ParsePrimaryPalette(metadata, textEntry),
                    _ => metadata
                };
            }

            return metadata;
        }

        private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");

        private static BsbMetadata ParseSize(BsbMetadata metadata, BsbTextEntry textEntry)
        {
            foreach (var line in textEntry.Lines)
            {
                var match = SizeRegex.Match(line);

                if (match.Success)
                {
                    int height = Int32.Parse(match.Groups["height"].Value);
                    int width = Int32.Parse(match.Groups["width"].Value);

                    return metadata with { Size = new BsbSize(height, width) };
                }
            }

            return metadata;
        }

        private static Regex PaletteRegex = new Regex("^(?<index>\\d+),(?<r>\\d+),(?<g>\\d+),(?<b>\\d+)$");

        private static BsbMetadata ParsePrimaryPalette(BsbMetadata metadata, BsbTextEntry textEntry)
        {
            foreach (var line in textEntry.Lines)
            {
                var match = PaletteRegex.Match(line);

                if (match.Success)
                {
                    byte index = Byte.Parse(match.Groups["index"].Value);
                    byte r = Byte.Parse(match.Groups["r"].Value);
                    byte g = Byte.Parse(match.Groups["g"].Value);
                    byte b = Byte.Parse(match.Groups["b"].Value);

                    // TODO: Think of a better way to copy existing palette.
                    return metadata with { Palette = new Dictionary<byte, BsbColor>(metadata.Palette ?? Enumerable.Empty<KeyValuePair<byte, BsbColor>>()) { { index, new BsbColor(r, g, b) } } };
                }
            }

            return metadata;
        }
    }
}