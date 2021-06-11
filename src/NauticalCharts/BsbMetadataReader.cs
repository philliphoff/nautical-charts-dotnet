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

            var entryMap = new Dictionary<string, IList<BsbTextEntry>>();

            foreach (var textEntry in textEntries)
            {
                IList<BsbTextEntry> entries;

                if (!entryMap.TryGetValue(textEntry.EntryType, out entries))
                {
                    entryMap[textEntry.EntryType] = entries = new List<BsbTextEntry>();
                }

                entries.Add(textEntry);
            }

            var metadata = new BsbMetadata();

            if (entryMap.TryGetValue("BSB", out IList<BsbTextEntry> bsbEntries))
            {
                metadata = ParseSize(metadata, bsbEntries);
            }

            if (entryMap.TryGetValue("RGB", out IList<BsbTextEntry> rgbEntries))
            {
                metadata = ParsePrimaryPalette(metadata, rgbEntries);
            }

            return metadata;
        }

        private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");

        private static BsbMetadata ParseSize(BsbMetadata metadata, IList<BsbTextEntry> textEntries)
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

        private static BsbMetadata ParsePrimaryPalette(BsbMetadata metadata, IList<BsbTextEntry> textEntries)
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