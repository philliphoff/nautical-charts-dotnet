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

            var entryMap =
                textEntries
                    .GroupBy(textEntry => textEntry.EntryType)
                    .ToDictionary(grouping => grouping.Key);

            var metadata = new BsbMetadata();

            var parsers = new Dictionary<string, Func<BsbMetadata, IEnumerable<BsbTextEntry>, BsbMetadata>>
            {
                { "BSB", ParseSize },
                { "PLY", ParseBorder },
                { "RGB", ParsePrimaryPalette }
            };

            foreach (var parser in parsers)
            {
                if (entryMap.TryGetValue(parser.Key, out IGrouping<string, BsbTextEntry> typeTextEntries))
                {
                    metadata = parser.Value(metadata, typeTextEntries);
                }
            }

            return metadata;
        }

        private static Regex BorderRegex = new Regex("^(?<order>\\d+),(?<latitude>[-+]?(\\d*\\.?\\d+|\\d+)),(?<longitude>[-+]?(\\d*\\.?\\d+|\\d+))$");

        private static BsbMetadata ParseBorder(BsbMetadata metadata, IEnumerable<BsbTextEntry> textEntries)
        {
            var border =
                textEntries
                    .SelectMany(textEntry => textEntry.Lines)
                    .Select(line => BorderRegex.Match(line))
                    .Where(match => match.Success)
                    .Select(
                        match =>
                        {
                            return new
                            {
                                Order = Int32.Parse(match.Groups["order"].Value),
                                Coordinate = new BsbCoordinate(
                                    Double.Parse(match.Groups["latitude"].Value),
                                    Double.Parse(match.Groups["longitude"].Value))
                            };
                        })
                    .OrderBy(item => item.Order)
                    .Select(item => item.Coordinate)
                    .ToList();

            if (border.Count > 0)
            {
                return metadata with { Border = border };
            }

            return metadata;
        }

        private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");

        private static BsbMetadata ParseSize(BsbMetadata metadata, IEnumerable<BsbTextEntry> textEntries)
        {
            var match =
                textEntries
                    .SelectMany(textEntry => textEntry.Lines)
                    .Select(line => SizeRegex.Match(line))
                    .FirstOrDefault(match => match.Success);

            if (match != null)
            {
                int height = Int32.Parse(match.Groups["height"].Value);
                int width = Int32.Parse(match.Groups["width"].Value);

                return metadata with { Size = new BsbSize(height, width) };
            }

            return metadata;
        }

        private static Regex PaletteRegex = new Regex("^(?<index>\\d+),(?<r>\\d+),(?<g>\\d+),(?<b>\\d+)$");

        private static BsbMetadata ParsePrimaryPalette(BsbMetadata metadata, IEnumerable<BsbTextEntry> textEntries)
        {
            var palette =
                textEntries
                    .SelectMany(textEntry => textEntry.Lines)
                    .Select(line => PaletteRegex.Match(line))
                    .Where(match => match.Success)
                    .ToDictionary(
                        match =>
                        {
                            return Byte.Parse(match.Groups["index"].Value);
                        },
                        match =>
                        {
                            byte r = Byte.Parse(match.Groups["r"].Value);
                            byte g = Byte.Parse(match.Groups["g"].Value);
                            byte b = Byte.Parse(match.Groups["b"].Value);

                            return new BsbColor(r, g, b);
                        });

            if (palette.Count > 0)
            {
                return metadata with { Palette = palette };
            }

            return metadata;
        }
    }
}